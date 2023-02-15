using NUnit.Framework;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;

namespace SerialLoops.Tests
{
    public class IntegrationTests
    {
        private Project _project;
        private Config _config;
        private string _zipPath;
        private string _dataDir;

        private HaruhiChokuretsuLib.Util.ConsoleLogger _log;
        private ConsoleProgressTracker _progressTracker;
        Platform _platform;

        [OneTimeSetUp]
        public void SetUp()
        {
            _zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "choku_data.zip");
            _dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "choku_data");

            _log = new();
            _progressTracker = new();

            // Download data archive
            _log.Log("Downloading data archive...");
            using HttpClient client = new();
            HttpRequestMessage request = new(HttpMethod.Get, "https://haroohie.blob.core.windows.net/serial-loops/choku_data.zip");
            HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult().EnsureSuccessStatusCode(); // Setup methods can't be async so...
            File.WriteAllBytes(_zipPath, response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()); // i'm so sorry

            // Extract data archive
            _log.Log("Extracting data archive...");
            using FileStream zipFile = File.OpenRead(_zipPath);
            ZipArchive zip = new(zipFile);
            zip.ExtractToDirectory(_dataDir);

            // Create a project using the extracted data
            _log.Log("Creating project...");
            _config = Config.LoadConfig(_log);
            _project = new("Tester", "en", _config, _log);

            string archivesDir = Path.Combine("original", "archives");
            string bgmDir = Path.Combine("original", "bgm");
            string vceDir = Path.Combine("original", "vce");
            Directory.CreateDirectory(Path.Combine(_project.BaseDirectory, archivesDir));
            Directory.CreateDirectory(Path.Combine(_project.IterativeDirectory, archivesDir));
            Directory.CreateDirectory(Path.Combine(_project.BaseDirectory, bgmDir));
            Directory.CreateDirectory(Path.Combine(_project.IterativeDirectory, bgmDir));
            Directory.CreateDirectory(Path.Combine(_project.BaseDirectory, vceDir));
            Directory.CreateDirectory(Path.Combine(_project.IterativeDirectory, vceDir));

            IO.CopyFiles(_dataDir, Path.Combine(_project.BaseDirectory, archivesDir));
            IO.CopyFiles(_dataDir, Path.Combine(_project.IterativeDirectory, archivesDir));
            IO.CopyFiles(Path.Combine(_dataDir, "bgm"), Path.Combine(_project.BaseDirectory, bgmDir));
            IO.CopyFiles(Path.Combine(_dataDir, "bgm"), Path.Combine(_project.IterativeDirectory, bgmDir));
            IO.CopyFiles(Path.Combine(_dataDir, "vce"), Path.Combine(_project.BaseDirectory, vceDir));
            IO.CopyFiles(Path.Combine(_dataDir, "vce"), Path.Combine(_project.IterativeDirectory, vceDir));

            // Load the project archives
            _project.LoadArchives(_log, _progressTracker);

            if (OperatingSystem.IsWindows())
            {
                //_platform = Eto;
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            File.Delete(_zipPath);
            Directory.Delete(_dataDir, true);
            Directory.Delete(_project.MainDirectory, true);
        }

        [Test]
        public void BackgroundTest()
        {
            foreach (BackgroundItem bg in _project.Items.Where(i => i.Type == ItemDescription.ItemType.Background).Cast<BackgroundItem>())
            {
                new Application(Platform.Detect).Run(new MainForm());
            }
        }
    }
}
