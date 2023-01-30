using System.IO;

namespace SerialLoops
{
    public class Project
    {
        public string Name { get; set; }
        public string MainDirectory { get; }
        public string BaseDirectory => Path.Combine(MainDirectory, "base");
        public string IterativeDirectory => Path.Combine(MainDirectory, "iterative");

        public Project(string name, Config config)
        {
            Name = name;
            MainDirectory = Path.Combine(config.ProjectsDirectory, name);

            Directory.CreateDirectory(MainDirectory);
            Directory.CreateDirectory(BaseDirectory);
            Directory.CreateDirectory(IterativeDirectory);
        }
    }
}
