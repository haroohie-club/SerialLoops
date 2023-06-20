using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Hacks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerialLoops.Lib
{
    public class Config
    {
        [JsonIgnore]
        public string ConfigPath { get; set; }
        public string UserDirectory { get; set; }
        [JsonIgnore]
        public string ProjectsDirectory => Path.Combine(UserDirectory, "Projects");
        [JsonIgnore]
        public string LogsDirectory => Path.Combine(UserDirectory, "Logs");
        [JsonIgnore]
        public string CachesDirectory => Path.Combine(UserDirectory, "Caches");
        [JsonIgnore]
        public string HacksDirectory => Path.Combine(UserDirectory, "Hacks");
        [JsonIgnore]
        public List<AsmHack> Hacks { get; set; }
        public string DevkitArmPath { get; set; }
        public bool UseDocker { get; set; }
        public string DevkitArmDockerTag { get; set; }
        public string EmulatorPath { get; set; }
        public bool AutoReopenLastProject { get; set; }
        public bool RememberProjectWorkspace { get; set; }
        public bool RemoveMissingProjects { get; set; }
        public bool CheckForUpdates { get; set; }
        public bool PreReleaseChannel { get; set; }

        public void Save(ILogger log)
        {
            IO.WriteStringFile(ConfigPath, JsonSerializer.Serialize(this), log);
        }

        public static Config LoadConfig(ILogger log)
        {
            string configJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (!File.Exists(configJson))
            {
                Config defaultConfig = GetDefault(log);
                defaultConfig.ValidateConfig(log);
                defaultConfig.ConfigPath = configJson;
                defaultConfig.InitializeHacks();
                IO.WriteStringFile(configJson, JsonSerializer.Serialize(defaultConfig), log);
                return defaultConfig;
            }

            try
            {
                Config config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configJson));
                config.ValidateConfig(log);
                config.ConfigPath = configJson;
                config.InitializeHacks();
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

        private void InitializeHacks()
        {
            if (!Directory.Exists(HacksDirectory))
            {
                Directory.CreateDirectory(HacksDirectory);
                IO.CopyFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks"), HacksDirectory);
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "hacks.json"), Path.Combine(HacksDirectory, "hacks.json"));
            }

            Hacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine(HacksDirectory, "hacks.json")));
            
            // Pull in new hacks in case we've updated the program with more
            List<AsmHack> builtinHacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "hacks.json")));
            IEnumerable<AsmHack> missingHacks = builtinHacks.Where(h => !Hacks.Contains(h));
            if (missingHacks.Any())
            {
                IO.CopyFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks"), HacksDirectory);
                Hacks.AddRange(missingHacks);
                File.WriteAllText(Path.Combine(HacksDirectory, "hacks.json"), JsonSerializer.Serialize(Hacks));
            }

            IEnumerable<AsmHack> updatedHacks = builtinHacks.Where(h => !Hacks.FirstOrDefault(o => h.Name == o.Name)?.DeepEquals(h) ?? false);
            if (updatedHacks.Any())
            {
                IO.CopyFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks"), HacksDirectory);
                foreach (AsmHack updatedHack in updatedHacks)
                {
                    Hacks[Hacks.FindIndex(h => h.Name == updatedHack.Name)] = updatedHack;
                }
                File.WriteAllText(Path.Combine(HacksDirectory, "hacks.json"), JsonSerializer.Serialize(Hacks));
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
                UserDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"),
                DevkitArmPath = devkitArmDir,
                EmulatorPath = emulatorPath,
                UseDocker = false,
                DevkitArmDockerTag = "latest",
                AutoReopenLastProject = true,
                RememberProjectWorkspace = true,
                RemoveMissingProjects = false,
                CheckForUpdates = true,
                PreReleaseChannel = false,
            };
        }
    }
}
