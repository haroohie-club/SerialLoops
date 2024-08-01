using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Hacks;
using SerialLoops.Lib.Script;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
        public string ScriptTemplatesDirectory => Path.Combine(UserDirectory, "ScriptTemplates");
        [JsonIgnore]
        public List<AsmHack> Hacks { get; set; }
        [JsonIgnore]
        public ObservableCollection<ScriptTemplate> ScriptTemplates { get; set; }
        public string CurrentCultureName { get; set; }
        public string DevkitArmPath { get; set; }
        public bool UseDocker { get; set; }
        public string DevkitArmDockerTag { get; set; }
        public string EmulatorPath { get; set; }
        public bool AutoReopenLastProject { get; set; }
        public bool RememberProjectWorkspace { get; set; }
        public bool RemoveMissingProjects { get; set; }
        public bool CheckForUpdates { get; set; }
        public bool PreReleaseChannel { get; set; }
        public string DisplayFont { get; set; }

        public void Save(ILogger log)
        {
            IO.WriteStringFile(ConfigPath, JsonSerializer.Serialize(this), log);
        }

        public static Config LoadConfig(Func<string, string> localize, ILogger log)
        {
            string configJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            if (!File.Exists(configJson))
            {
                Config defaultConfig = GetDefault(log);
                defaultConfig.ValidateConfig(localize, log);
                defaultConfig.ConfigPath = configJson;
                defaultConfig.InitializeHacks(log);
                defaultConfig.InitializeScriptTemplates(localize, log);
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

        public void ValidateConfig(Func<string, string> localize, ILogger log)
        {
            if (string.IsNullOrWhiteSpace(DevkitArmPath))
            {
                log.LogError(localize("devkitARM is not detected at the default or specified install location. Please set devkitARM path."));
            }
            if (CurrentCultureName is null)
            {
                CurrentCultureName = CultureInfo.CurrentCulture.Name;
            }
            else
            {
                CultureInfo.CurrentCulture = new(CurrentCultureName);
            }
        }

        private void InitializeHacks(ILogger log)
        {
            if (!Directory.Exists(HacksDirectory))
            {
                Directory.CreateDirectory(HacksDirectory);
                IO.CopyFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks"), HacksDirectory, log);
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "hacks.json"), Path.Combine(HacksDirectory, "hacks.json"));
            }

            Hacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine(HacksDirectory, "hacks.json")));
            
            // Pull in new hacks in case we've updated the program with more
            List<AsmHack> builtinHacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "hacks.json")));
            IEnumerable<AsmHack> missingHacks = builtinHacks.Where(h => !Hacks.Contains(h));
            if (missingHacks.Any())
            {
                IO.CopyFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks"), HacksDirectory, log);
                Hacks.AddRange(missingHacks);
                File.WriteAllText(Path.Combine(HacksDirectory, "hacks.json"), JsonSerializer.Serialize(Hacks));
            }

            IEnumerable<AsmHack> updatedHacks = builtinHacks.Where(h => !Hacks.FirstOrDefault(o => h.Name == o.Name)?.DeepEquals(h) ?? false);
            if (updatedHacks.Any())
            {
                IO.CopyFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Hacks"), HacksDirectory, log);
                foreach (AsmHack updatedHack in updatedHacks)
                {
                    Hacks[Hacks.FindIndex(h => h.Name == updatedHack.Name)] = updatedHack;
                }
                File.WriteAllText(Path.Combine(HacksDirectory, "hacks.json"), JsonSerializer.Serialize(Hacks));
            }
        }

        private void InitializeScriptTemplates(Func<string, string> localize, ILogger log)
        {
            if (!Directory.Exists(ScriptTemplatesDirectory))
            {
                Directory.CreateDirectory(ScriptTemplatesDirectory);
            }

            IO.CopyFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "ScriptTemplates"), ScriptTemplatesDirectory, log); // Update script templates each time we launch in case of program update
            string[] scriptTemplateFiles = Directory.GetFiles(ScriptTemplatesDirectory, "*.slscr");
            List<ScriptTemplate> templates = [];
            foreach (string scriptTemplateFile in scriptTemplateFiles)
            {
                try
                {
                    templates.Add(JsonSerializer.Deserialize<ScriptTemplate>(File.ReadAllText(scriptTemplateFile)));
                }
                catch (Exception ex)
                {
                    log.LogException(string.Format(localize("Failed to deserialize script template file '{0}'"), scriptTemplateFile), ex);
                }
            }
            ScriptTemplates = new(templates);
        }

        private static Config GetDefault(ILogger log)
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
            string emulatorPath = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                emulatorPath = Path.Combine("/Applications", "melonDS.app");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                emulatorPath = Path.Combine("/snap", "melonds", "current", "usr", "local", "bin", "melonDS");
            }
            if (!Directory.Exists(emulatorPath) && !File.Exists(emulatorPath)) // on Mac, .app is a dir, so we check both of these
            {
                emulatorPath = "";
                log.LogWarning("Valid emulator path not found in config.json.");
            }

            return new Config
            {
                UserDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops"),
                CurrentCultureName = CultureInfo.CurrentCulture.Name,
                DevkitArmPath = devkitArmDir,
                EmulatorPath = emulatorPath,
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
}
