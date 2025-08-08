using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
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

    public Config LoadConfig(Func<string, string> localize, ILogger log)
    {
        string configJson = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SerialLoops", "config.json");

        if (!File.Exists(configJson))
        {
            Config defaultConfig = GetDefault(log);
            defaultConfig.ValidateConfig(localize, log);
            defaultConfig.ConfigPath = configJson;
            defaultConfig.InitializeHacks(log);
            defaultConfig.InitializeScriptTemplates(localize, log);
            if (!Directory.Exists(Path.GetDirectoryName(configJson)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configJson)!);
            }
            IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfig), log);
            return defaultConfig;
        }

        try
        {
            Config config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configJson));
            config.ValidateConfig(localize, log);
            config.ConfigPath = configJson;
            config.InitializeHacks(log);
            config.InitializeScriptTemplates(localize, log);
            return config;
        }
        catch (JsonException exc)
        {
            log.LogException(localize("Exception occurred while parsing config.json!"), exc);
            Config defaultConfig = GetDefault(log);
            defaultConfig.ValidateConfig(localize, log);
            IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfig), log);
            return defaultConfig;
        }
    }

    public static Config GetDefault(ILogger log)
    {
        string llvmDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine("C:", "Program Files", "LLVM")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine("/opt", "homebrew", "opt", "llvm")
                : PatchableConstants.LinuxDefaultLlvmDir;
        if (!Directory.Exists(llvmDir))
        {
            llvmDir = string.Empty;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Go this direction so that if (for some reason) multiple LLVM installations exist, we use the latest one
                for (int i = LlvmMaxVersion; i >= LlvmMinVersion; i--)
                {
                    if (!Directory.Exists($"{PatchableConstants.LinuxDefaultLlvmDir}-{i}"))
                    {
                        continue;
                    }

                    llvmDir = $"{PatchableConstants.LinuxDefaultLlvmDir}-{i}";
                    break;
                }
            }
        }

        string ninjaPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ninja.exe")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine("/opt/homebrew/bin/ninja")
                : Path.Combine("/usr/bin/ninja");
        if (!File.Exists(ninjaPath))
        {
            ninjaPath = string.Empty;
        }

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
                    StartInfo = new(PatchableConstants.FlatpakProcess, [..PatchableConstants.FlatpakProcessBaseArgs, "info", emulatorFlatpak])
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
        if (!emulatorExists) // on Mac, .app is a dir, so we check both of these
        {
            emulatorPath = string.Empty;
            emulatorFlatpak = string.Empty;
            log.LogWarning("Valid emulator path not found in config.json.");
        }

        return new()
        {
            UserDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"),
            CurrentCultureName = CultureInfo.CurrentCulture.Name,
            LlvmPath = llvmDir,
            NinjaPath = ninjaPath,
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
