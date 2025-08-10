using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Factories;

// Factories are stupid, you say. What's the point of instantiating an object
// just to do what a static method could have (and indeed, had) done?
// I bow my head and chuckle. "Young one," I rasp out, my head flinging up
// so you begin to sink into the whites of my bulging eyes.
//
// "You've forgotten the mocks."
public class ConfigFactory : IConfigFactory
{
    private const int LlvmMinVersion = 17;
    private const int LlvmMaxVersion = 21;

    public ConfigUser LoadConfig(Func<string, string> localize, ILogger log)
    {
        string configJson = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SerialLoops", "config.json");
        string sysConfigJson = Environment.GetEnvironmentVariable(EnvironmentVariables.SysConfigPath) ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SerialLoops", "sysconfig.json");

        ConfigSystem sysConfig;
        if (!File.Exists(sysConfigJson))
        {
            sysConfig = GetDefaultSystem(log);
            if (!Directory.Exists(Path.GetDirectoryName(sysConfigJson)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configJson)!);
            }

            IO.WriteStringFile(sysConfigJson, JsonSerializer.Serialize(sysConfig), log);
        }
        else
        {
            try
            {
                sysConfig = JsonSerializer.Deserialize<ConfigSystem>(File.ReadAllText(configJson));
            }
            catch (JsonException exc)
            {
                log.LogException(localize("Exception occurred while parsing sysconfig.json!"), exc);
                sysConfig = GetDefaultSystem(log);
                IO.WriteStringFile(sysConfigJson, JsonSerializer.Serialize(sysConfig), log);
            }
        }

        if (!File.Exists(configJson))
        {
            ConfigUser defaultConfigUser = GetDefault(sysConfig, log);
            defaultConfigUser.ValidateConfig(localize, log);
            defaultConfigUser.ConfigPath = configJson;
            defaultConfigUser.InitializeHacks(log);
            defaultConfigUser.InitializeScriptTemplates(localize, log);
            if (!Directory.Exists(Path.GetDirectoryName(configJson)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configJson)!);
            }
            IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfigUser), log);
            return defaultConfigUser;
        }

        try
        {
            ConfigUser configUser = JsonSerializer.Deserialize<ConfigUser>(File.ReadAllText(configJson));
            configUser.SysConfig = sysConfig;
            configUser.ValidateConfig(localize, log);
            configUser.ConfigPath = configJson;
            configUser.InitializeHacks(log);
            configUser.InitializeScriptTemplates(localize, log);
            return configUser;
        }
        catch (JsonException exc)
        {
            log.LogException(localize("Exception occurred while parsing config.json!"), exc);
            ConfigUser defaultConfigUser = GetDefault(sysConfig, log);
            defaultConfigUser.ValidateConfig(localize, log);
            IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfigUser), log);
            return defaultConfigUser;
        }
    }

    public static ConfigSystem GetDefaultSystem(ILogger log)
    {
        string llvmDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine("C:", "Program Files", "LLVM")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine("/opt", "homebrew", "opt", "llvm")
                : Environment.GetEnvironmentVariable(EnvironmentVariables.LlvmPath) ?? "/usr/lib/llvm";
        if (!Directory.Exists(llvmDir))
        {
            llvmDir = string.Empty;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Go this direction so that if (for some reason) multiple LLVM installations exist, we use the latest one
                for (int i = LlvmMaxVersion; i >= LlvmMinVersion; i--)
                {
                    if (!Directory.Exists($"/usr/lib/llvm-{i}"))
                    {
                        continue;
                    }

                    llvmDir = $"/usr/liv/llvm-{i}";
                    break;
                }

                if (string.IsNullOrEmpty(llvmDir) && File.Exists("/usr/bin/clang"))
                {
                    llvmDir = "/usr/bin";
                }
            }
        }

        string ninjaPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(AppContext.BaseDirectory, "ninja.exe")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine("/opt/homebrew/bin/ninja")
                : Environment.GetEnvironmentVariable(EnvironmentVariables.NinjaPath) ?? Path.Combine("/usr/bin/ninja");
        if (!File.Exists(ninjaPath))
        {
            ninjaPath = string.Empty;
        }
        string flatpakProcess = Environment.GetEnvironmentVariable(EnvironmentVariables.FlatpakProcess) ?? "flatpak";
        string[] flatpakProcessBaseArgs = Environment.GetEnvironmentVariable(EnvironmentVariables.FlatpakProcArg)?.Split(' ') ?? [];

        string flatpakRunProcess = Environment.GetEnvironmentVariable(EnvironmentVariables.FlatpakRunProcess) ?? string.Empty;
        string[] flatpakRunProcessBaseArgs = Environment.GetEnvironmentVariable(EnvironmentVariables.FlatpakRunProcArg)?.Split(' ') ?? [];

        bool useUpdater = Environment.GetEnvironmentVariable(EnvironmentVariables.UseUpdater)
            ?.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ?? false;

        return new()
        {
            LlvmPath = llvmDir,
            NinjaPath = ninjaPath,
            FlatpakProcess = flatpakProcess,
            FlatpakProcessBaseArgs = flatpakProcessBaseArgs,
            FlatpakRunProcess = flatpakRunProcess,
            FlatpakRunProcessBaseArgs = flatpakRunProcessBaseArgs,
            UseUpdater = useUpdater,
        };
    }

    public static ConfigUser GetDefault(ConfigSystem sysConfig, ILogger log)
    {
        bool emulatorExists = false;
        string emulatorPath = string.Empty;
        string emulatorFlatpak = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            emulatorPath = Path.Combine("/Applications", "melonDS.app");
            emulatorExists = Directory.Exists(emulatorPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            emulatorFlatpak = "net.kuribo64.melonDS";
            try
            {
                Process flatpakProc = new()
                {
                    StartInfo = new(sysConfig.FlatpakProcess, [..sysConfig.FlatpakProcessBaseArgs, "info", emulatorFlatpak])
                    {
                        RedirectStandardError = true, RedirectStandardOutput = true,
                    },
                };
                flatpakProc.OutputDataReceived += (_, args) => log.Log(args.Data ?? string.Empty);
                flatpakProc.ErrorDataReceived += (_, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        log.LogWarning(args.Data);
                    }
                };
                flatpakProc.Start();
                flatpakProc.WaitForExit();
                emulatorExists = flatpakProc.ExitCode == 0;
            }
            catch
            {
                emulatorExists = false;
            }
        }
        if (!emulatorExists)
        {
            emulatorPath = string.Empty;
            emulatorFlatpak = string.Empty;
            log.LogWarning("Valid emulator path not found in config.json.");
        }

        return new()
        {
            SysConfig = sysConfig,
            UserDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"),
            CurrentCultureName = CultureInfo.CurrentCulture.Name,
            EmulatorPath = emulatorPath,
            EmulatorFlatpak = emulatorFlatpak,
            AutoReopenLastProject = false,
            RememberProjectWorkspace = true,
            RemoveMissingProjects = false,
            CheckForUpdates = true,
            PreReleaseChannel = false,
        };
    }
}
