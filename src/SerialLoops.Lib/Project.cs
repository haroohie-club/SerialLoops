using SerialLoops.Lib.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SerialLoops.Lib
{
    public class Project
    {
        public string Name { get; set; }
        public string LangCode { get; set; }
        public string MainDirectory { get; }
        public string BaseDirectory => Path.Combine(MainDirectory, "base");
        public string IterativeDirectory => Path.Combine(MainDirectory, "iterative");

        public Project(string name, string langCode, Config config, ILogger log)
        {
            Name = name;
            LangCode = langCode;
            MainDirectory = Path.Combine(config.ProjectsDirectory, name);
            log.Log("Creating project directories...");
            try
            {
                Directory.CreateDirectory(MainDirectory);
                File.WriteAllText(Path.Combine(MainDirectory, $"{Name}.seproj"), JsonSerializer.Serialize(this));
                Directory.CreateDirectory(BaseDirectory);
                Directory.CreateDirectory(IterativeDirectory);
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred while attempting to create project directories.\n{exc.Message}\n\n{exc.StackTrace}");
            }
        }

        public static Project OpenProject(string projFile, Config config, ILogger log)
        {
            log.Log($"Loading project from '{projFile}'...");
            Project project = JsonSerializer.Deserialize<Project>(projFile);
            return project;
        }

        public static Dictionary<string, string> AvailableLanguages = new()
        {
            { "English", "en" },
            { "Japanese", "ja" },
            { "Russian", "ru" },
            { "Spanish", "es" },
            { "Portuguese (Brazilian)", "pt-BR" },
            { "Italian", "it" },
            { "French", "fr" },
            { "German", "de" },
            { "Greek", "el" },
        };
    }
}
