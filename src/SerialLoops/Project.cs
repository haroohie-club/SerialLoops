using System.IO;

namespace SerialLoops
{
    public class Project
    {
        public string Name { get; set; }
        public string MainDirectory { get; }
        public string BaseDirectory => Path.Combine(MainDirectory, "base");
        public string IterativeDirectory => Path.Combine(MainDirectory, "iterative");

        public Project(string name, Config config, bool createDirectories = true)
        {
            Name = name;
            MainDirectory = Path.Combine(config.ProjectsDirectory, name);

            if (createDirectories)
            {
                Directory.CreateDirectory(MainDirectory);
                Directory.CreateDirectory(BaseDirectory);
                Directory.CreateDirectory(IterativeDirectory);
            }
        }

        public static Project OpenProject(string name, Config config)
        {
            Project project = new(name, config, createDirectories: false);
            return project;
        }
    }
}
