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
        string devkitArmDir = Environment.GetEnvironmentVariable("DEVKITARM") ?? string.Empty;
        if (!string.IsNullOrEmpty(devkitArmDir) && !Directory.Exists(devkitArmDir))
        {
            devkitArmDir = "";
        }
        if (string.IsNullOrEmpty(devkitArmDir))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                devkitArmDir = Path.Combine("C:", "devkitPro", "devkitARM");
            }
            else
            {
                devkitArmDir = Path.Combine("/opt", "devkitpro", "devkitARM");
            }
        }
        if (!Directory.Exists(devkitArmDir))
        {
            devkitArmDir = "";
        }

        // TODO: Probably make a way of defining "presets" of common emulator install paths on different platforms.
        // Ideally this should be as painless as possible.
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
                Process flatpakProc = Process.Start((new ProcessStartInfo("flatpak", ["info", emulatorFlatpak])));
                flatpakProc?.WaitForExit();
                emulatorExists = flatpakProc?.ExitCode == 0;
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

        return new Config
        {
            UserDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"),
            CurrentCultureName = CultureInfo.CurrentCulture.Name,
            DevkitArmPath = devkitArmDir,
            EmulatorPath = emulatorPath,
            EmulatorFlatpak = emulatorFlatpak,
            UseDocker = false,
            DevkitArmDockerTag = "latest",
            AutoReopenLastProject = false,
            RememberProjectWorkspace = true,
            RemoveMissingProjects = false,
            CheckForUpdates = true,
            PreReleaseChannel = false,
        };
    }
}