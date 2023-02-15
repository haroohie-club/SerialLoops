using Eto.Drawing;
using Eto.Forms;
using Eto.UnitTest;
using Eto.UnitTest.Runners;
using Eto.UnitTest.UI;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Tests;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;

namespace SerialLoops.Test.Wpf
{
    public class MainForm : Form
    {
        private Project _project;
        private Config _config;
        private string _zipPath;
        private string _dataDir;

        private HaruhiChokuretsuLib.Util.ConsoleLogger _log;
        private ConsoleProgressTracker _progressTracker;

        static MainForm()
        {
            Eto.UnitTest.NUnit.NUnitTestRunnerType.Register();
        }

        public MainForm()
        {
            Title = $"CodeEditor Test, Platform: {Platform.ID}";
            ClientSize = new Size(1400, 800);

            Menu = new MenuBar(); // show standard macOS menu.

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

            var editor = new BackgroundEditor((BackgroundItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Background), _log);

            Action<BackgroundEditor, string> pp = (f, pfx) => MessageBox.Show($"{pfx}: name: {editor}, size: {editor}");

            var btn = new Button { Text = "Font" };
            btn.Click += (s, e) =>
            {
                var originalEditor = editor;
                pp(originalEditor, "first");
            };


            var tests = new UnitTestPanel(true);

            this.LoadComplete += async (s, e) =>
            {
                var testSource = new TestSource(System.Reflection.Assembly.GetExecutingAssembly());
                var mtr = new Eto.UnitTest.Runners.MultipleTestRunner();
                await mtr.Load(testSource);
                tests.Runner = new LoggingTestRunner(mtr);
                WpfTests.editor = editor;

            };

            var splitter = new Splitter
            {
                Panel1 = tests,
                Panel2 = new Splitter { Panel1 = editor, Orientation = Orientation.Vertical, Panel1MinimumSize = 100, FixedPanel = SplitterFixedPanel.Panel2 }
            };
            Content = new TableLayout { Rows = { splitter } };
        }
    }
}
