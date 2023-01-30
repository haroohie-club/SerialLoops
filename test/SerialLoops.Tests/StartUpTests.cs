using NUnit.Framework;
using SerialLoops.Lib;
using SerialLoops.Lib.Logging;
using System.IO;

namespace SerialLoops.Tests
{
    public class StartUpTests
    {
        private ConsoleLogger _log;

        [OneTimeSetUp]
        public void SetUp()
        {
            _log = new();
        }

        [Test]
        public void ConfigCreationTest()
        {
            // Create default config
            Config config = Config.LoadConfig(_log);
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(config.ConfigPath), $"Config file not found at '{config.ConfigPath}'");
                Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(config.ConfigPath), "SerialLoops.Lib.dll")),
                    $"SerialLoops library DLL not found at '{Path.Combine(Path.GetDirectoryName(config.ConfigPath), "SerialLoops.Lib.dll")}'");
            });

            // Change config and save it
            config.ProjectsDirectory = "testTime";
            config.Save(_log);

            // Load config from disk
            Config newConfig = Config.LoadConfig(_log);
            Assert.That(newConfig.ProjectsDirectory, Is.EqualTo(config.ProjectsDirectory));

            File.Delete(config.ConfigPath);
        }

        [Test]
        public void ProjectCreationTest()
        {
            Config config = Config.LoadConfig(_log);
            Project project = new("Test", config, _log);

            Assert.Multiple(() =>
            {
                Assert.That(Directory.Exists(project.MainDirectory));
                Assert.That(Directory.Exists(project.BaseDirectory));
                Assert.That(Directory.Exists(project.IterativeDirectory));
            });

            Directory.Delete(project.MainDirectory, true);
        }
    }
}