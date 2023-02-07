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
                Config defaultConfig = GetDefault();
                defaultConfig.ConfigPath = configJson;
                IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfig), log);
                return defaultConfig;
            }
            else
            {
                try
                {
                    Config config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configJson));
                    config.ConfigPath = configJson;
                    return config;
                }
                catch (JsonException exc)
                {
                    log.LogError($"Exception occurred while parsing config.json!\n{exc.Message}\n\n{exc.StackTrace}");
                    Config defaultConfig = GetDefault();
                    IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfig), log);
                    return defaultConfig;
                }
            }
        }

        private static Config GetDefault()
        {
            string devkitArmDir;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                devkitArmDir = Path.Combine("C:", "devkitPro", "devkitARM");
            }
            else
            {
                devkitArmDir = Path.Combine("opt", "devkitpro", "devkitARM");
            }
            if (!Directory.Exists(devkitArmDir))
            {
                devkitArmDir = "";
            }

            return new Config
            {
                ProjectsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"),
                DevkitArmPath = devkitArmDir,
            };
        }
    }
}
