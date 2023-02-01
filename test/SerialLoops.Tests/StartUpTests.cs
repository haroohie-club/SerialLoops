using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using SerialLoops.Lib;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

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

        private async Task<Project> DownloadTestRom()
        {
            Config config = Config.LoadConfig(_log);
            Project project = new("Test", "en", config, _log);

            string romPath = Path.Combine(project.MainDirectory, "bcsds.nds");
            HttpClient client = new();
            File.WriteAllBytes(romPath, await client.GetByteArrayAsync("https://github.com/WiIIiam278/BCSDS/releases/download/1.0/bcsds.nds"));

            IO.OpenRom(project, romPath);

            return project;
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
            Project project = new("Test", "en", config, _log);

            Assert.Multiple(() =>
            {
                Assert.That(Directory.Exists(project.MainDirectory));
                Assert.That(Directory.Exists(project.BaseDirectory));
                Assert.That(Directory.Exists(project.IterativeDirectory));
            });

            Directory.Delete(project.MainDirectory, true);
        }

        [Test]
        public async Task RomUnpackingTest()
        {
            Project project = await DownloadTestRom();

            Assert.Multiple(() =>
            {
                Assert.That(Directory.Exists(Path.Combine(project.BaseDirectory, "original", "archives")), $"Directory {Path.Combine(project.BaseDirectory, "original", "archives")} did not exist.");
                Assert.That(Directory.Exists(Path.Combine(project.BaseDirectory, "original", "overlay")), $"Directory {Path.Combine(project.BaseDirectory, "original", "overlay")} did not exist.");
                Assert.That(Directory.Exists(Path.Combine(project.BaseDirectory, "rom", "data", "sprites")), $"Directory {Path.Combine(project.BaseDirectory, "rom", "data", "sprites")} did not exist.");
                Assert.That(Directory.Exists(Path.Combine(project.BaseDirectory, "rom", "overlay")), $"Directory {Path.Combine(project.BaseDirectory, "rom", "data", "overlay")} did not exist.");
                Assert.That(File.Exists(Path.Combine(project.BaseDirectory, "rom", "arm9.bin")), $"File {Path.Combine(project.BaseDirectory, "rom", "arm9.bin")} did not exist.");
                Assert.That(File.Exists(Path.Combine(project.BaseDirectory, "rom", "Test.xml")), $"File {Path.Combine(project.BaseDirectory, "rom", "Test.xml")} did not exist.");
                Assert.That(File.Exists(Path.Combine(project.BaseDirectory, "src", "arm9.bin")), $"File {Path.Combine(project.BaseDirectory, "src", "arm9.bin")} did not exist.");
                Assert.That(File.Exists(Path.Combine(project.BaseDirectory, "src", "linker.x")), $"File {Path.Combine(project.BaseDirectory, "src", "linker.x")} did not exist.");
                Assert.That(File.Exists(Path.Combine(project.BaseDirectory, "src", "Makefile")), $"File {Path.Combine(project.BaseDirectory, "src", "Makefile")} did not exist.");
                Assert.That(File.Exists(Path.Combine(project.BaseDirectory, "src", "overlays", "linker.x")), $"File {Path.Combine(project.BaseDirectory, "src", "overlays", "linker.x")} did not exist.");
                Assert.That(File.Exists(Path.Combine(project.BaseDirectory, "src", "overlays", "Makefile")), $"File {Path.Combine(project.BaseDirectory, "src", "overlays", "Makefile")} did not exist.");
            });

            Directory.Delete(project.MainDirectory, true);
        }
    }
}