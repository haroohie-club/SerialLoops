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
        }
    }
}