using SerialLoops.Lib.Logging;
using System;
using System.IO;

namespace SerialLoops.Lib
{
    public class Project
    {
        public string Name { get; set; }
        public string MainDirectory { get; }
        public string BaseDirectory => Path.Combine(MainDirectory, "base");
        public string IterativeDirectory => Path.Combine(MainDirectory, "iterative");

        public Project(string name, Config config, ILogger log, bool createDirectories = true)
        {
            Name = name;
            MainDirectory = Path.Combine(config.ProjectsDirectory, name);

            if (createDirectories)
            {
                log.Log("Creating project directories...");
                try
                {
                    Directory.CreateDirectory(MainDirectory);
                    Directory.CreateDirectory(BaseDirectory);
                    Directory.CreateDirectory(IterativeDirectory);
                }
                catch (Exception exc)
                {
                    log.LogError($"Exception occurred while attempting to create project directories.\n{exc.Message}\n\n{exc.StackTrace}");
                }
            }
        }

        public static Project OpenProject(string name, Config config, ILogger log)
        {
            Project project = new(name, config, log, createDirectories: false);
            return project;
        }
    }
}
