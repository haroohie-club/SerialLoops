using Microsoft.VisualBasic.Logging;
using NUnit.Framework;
using SerialLoops.Lib.Util;
using SerialLoops.Lib;
using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System;
using SerialLoops.Lib.Items;
using System.Linq;
using SerialLoops.Editors;
using Eto.Forms;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal;
using System.Runtime.ExceptionServices;

namespace SerialLoops.Tests.Wpf
{
    public class Tests
    {
        private Project _project;
        private Config _config;
        private string _zipPath;
        private string _dataDir;

        private Application _application;
        private HaruhiChokuretsuLib.Util.ConsoleLogger _log;
        private ConsoleProgressTracker _progressTracker;

        [OneTimeSetUp]
        public void Setup()
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
            zip.ExtractToDirectory(_dataDir, overwriteFiles: true);

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

            _application = new(new Eto.Wpf.Platform());
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            File.Delete(_zipPath);
            Directory.Delete(_dataDir, true);
            Directory.Delete(_project.MainDirectory, true);
        }

        [Test, InvokeOnUI, Apartment(ApartmentState.STA)]
        public void Test1()
        {
            foreach (BackgroundItem bg in _project.Items.Where(i => i.Type == ItemDescription.ItemType.Background).Cast<BackgroundItem>())
            {
                BackgroundEditor editor = new(bg, _log);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class InvokeOnUIAttribute : Attribute, IWrapSetUpTearDown
    {
        public TestCommand Wrap(TestCommand command) => new RunOnUICommand(command);

        class RunOnUICommand : DelegatingTestCommand
        {
            public RunOnUICommand(TestCommand innerCommand)
                : base(innerCommand)
            {
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                Exception exception = null;

                var result = Application.Instance.Invoke(() =>
                {
                    try
                    {
                        context.EstablishExecutionEnvironment();
                        return innerCommand.Execute(context);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        return null;
                    }
                });

                if (exception != null)
                {
                    ExceptionDispatchInfo.Capture(exception).Throw();
                }

                return result;
            }
        }
    }
}