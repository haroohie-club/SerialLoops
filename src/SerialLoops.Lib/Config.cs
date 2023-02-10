using HaruhiChokuretsuLib.Util;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SerialLoops.Lib
{
    public class Config
    {
        public string ConfigPath { get; set; }
        public string ProjectsDirectory { get; set; }
        public string DevkitArmPath { get; set; }
        public string EmulatorPath { get; set; }

        public void Save(ILogger log)
        {
            log.Log($"Saving config to '{ConfigPath}'...");
            IO.WriteStringFile(ConfigPath, JsonSerializer.Serialize(this), log);
        }

        public static Config LoadConfig(ILogger log)
        {
            string configJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (!File.Exists(configJson))
            {
                log.Log($"Creating default config at '{configJson}'...");
                Config defaultConfig = GetDefault(log);
                defaultConfig.ValidateConfig(log);
                defaultConfig.ConfigPath = configJson;
                IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfig), log);
                return defaultConfig;
            }

            try
            {
                Config config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configJson));
                config.ValidateConfig(log);
                config.ConfigPath = configJson;
                return config;
            }
            catch (JsonException exc)
            {
                log.LogError($"Exception occurred while parsing config.json!\n{exc.Message}\n\n{exc.StackTrace}");
                Config defaultConfig = GetDefault(log);
                defaultConfig.ValidateConfig(log);
                IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfig), log);
                return defaultConfig;
            }
        }

        public void ValidateConfig(ILogger log)
        {
            if (string.IsNullOrWhiteSpace(DevkitArmPath))
            {
                log.LogError("devkitARM is not detected at the default or specified install location. Please set devkitPro path.");
            }
        }

        private static Config GetDefault(ILogger log)
        {
            string devkitArmDir;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                devkitArmDir = Path.Combine("C:", "devkitPro", "devkitARM");
            }
            else
            {
                devkitArmDir = Path.Combine("/opt", "devkitpro", "devkitARM");
            }
            if (!Directory.Exists(devkitArmDir))
            {
                devkitArmDir = "";
            }

            // TODO: Probably make a way of defining "presets" of common emulator install paths on different platforms.
            // Ideally this should be as painless as possible.
            string emulatorPath = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                emulatorPath = Path.Combine("/Applications", "melonDS.app");
            }
            if (!Directory.Exists(emulatorPath))
            {
                emulatorPath = "";
                log.LogWarning("Valid emulator path not found in config.json.");
            }

            return new Config
            {
                ProjectsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"),
                DevkitArmPath = devkitArmDir,
                EmulatorPath = emulatorPath
            };
        }
    }
}
