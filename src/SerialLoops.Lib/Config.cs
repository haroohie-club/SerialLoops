using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Hacks;
using SerialLoops.Lib.Script;

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

        internal void InitializeHacks(ILogger log)
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

        internal void InitializeScriptTemplates(Func<string, string> localize, ILogger log)
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
    }
}
