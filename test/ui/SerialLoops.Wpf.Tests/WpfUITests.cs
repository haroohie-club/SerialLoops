using HaruhiChokuretsuLib.Archive.Graphics;
using NAudio.Wave;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util.WaveformRenderer;
using SerialLoops.Tests.Shared;
using SimpleImageComparisonClassLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;

namespace SerialLoops.Wpf.Tests
{
    public class WpfUITests
    {
        private WindowsDriver<WindowsElement> _driver;
        private UiVals? _uiVals;
        private Project? _project;
        private readonly UiTestLogger _logger = new(Environment.GetEnvironmentVariable("LOG_FILE") ?? "testlog_console.log");
        private readonly ConsoleProgressTracker _tracker = new();
        private Process? _wad;
        private string _testAssets = string.Empty;

        private const string TEST_BG_ASSET = "lizmelo-painting.png";

        [OneTimeSetUp]
        public void Setup()
        {
            // To run these tests locally, you can create a file called 'ui_vals.json' and place it next to the test assembly (in the output folder)

            if (File.Exists("ui_vals.json"))
            {
                _uiVals = JsonSerializer.Deserialize<UiVals>(File.ReadAllText("ui_vals.json")) ?? new();
                if (!Directory.Exists(_uiVals.ArtifactsDir))
                {
                    Directory.CreateDirectory(_uiVals.ArtifactsDir);
                }
            }
            else
            {
                string romUri = Environment.GetEnvironmentVariable("ROM_URI") ?? string.Empty;
                string romPath = Path.Combine(Directory.GetCurrentDirectory(), "HaruhiChokuretsu.nds");
                HttpClient httpClient = new();
                using Stream downloadStream = httpClient.Send(new() { Method = HttpMethod.Get, RequestUri = new(romUri) }).Content.ReadAsStream();
                using FileStream fileStream = new(romPath, FileMode.Create);
                downloadStream.CopyTo(fileStream);
                fileStream.Flush();

                _uiVals = new()
                {
                    AppLoc = Environment.GetEnvironmentVariable("APP_LOCATION") ?? string.Empty,
                    ProjectName = Environment.GetEnvironmentVariable("PROJECT_NAME") ?? "WinUITest",
                    WinAppDriverLoc = Environment.GetEnvironmentVariable("WINAPPDRIVER_LOC") ?? string.Empty,
                    RomLoc = romPath,
                    ArtifactsDir = Environment.GetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY") ?? "artifacts",
                };
            }
            _logger.Log(JsonSerializer.Serialize(_uiVals, new JsonSerializerOptions { WriteIndented = true }));

            _testAssets = TestAssetsDownloader.DownloadTestAssets().GetAwaiter().GetResult();

            if (!string.IsNullOrEmpty(_uiVals.WinAppDriverLoc))
            {
                _logger.Log($"Attempting to launch WinAppDriver from '{_uiVals.WinAppDriverLoc}'");
                _wad = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"{_uiVals.WinAppDriverLoc}\"",
                    UseShellExecute = true,
                });
                Thread.Sleep(TimeSpan.FromSeconds(2)); // Give WAD time to launch before continuing
            }

            Uri serverUri = new(Environment.GetEnvironmentVariable("APPIUM_HOST") ?? "http://127.0.0.1:4723/");

            AppiumOptions driverOptions = new();
            driverOptions.AddAdditionalCapability("app", _uiVals!.AppLoc);
            driverOptions.AddAdditionalCapability("deviceName", "WindowsPC");

            _driver = new(serverUri, driverOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1.5);

            Thread.Sleep(TimeSpan.FromSeconds(3));
            _driver.SwitchTo().Window(_driver.WindowHandles.First()); // Switch to the update available dialog
            Thread.Sleep(100); // Give it time
            _driver.FindElementByName("Skip Update").Click(); // close the dialog

            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            _driver.FindElementByName("Maximize").Click();
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals.ArtifactsDir, "start.png"));
            _driver.FindElementByClassName("Hyperlink").Click();
            Actions actions = new(_driver);
            actions.SendKeys(_uiVals.ProjectName);
            actions.Build().Perform();
            _driver.FindElementByName("Open ROM").Click();
            actions = new(_driver);
            actions.SendKeys(_uiVals.RomLoc);
            actions.SendKeys(Keys.Enter);
            actions.Build().Perform();
            _driver.FindElementByName("Create").Click();
            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            } while (_driver.WindowHandles.Count > 1);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals.ArtifactsDir, "project_open.png"));

            _logger.Log("Loading project...");
            string configPath = Path.Combine(Path.GetDirectoryName(_uiVals.AppLoc) ?? string.Empty, "config.json");
            Config config = File.Exists(configPath) ? (JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath)) ?? Config.LoadConfig(_logger)) : Config.LoadConfig(_logger);
            (_project, _) = Project.OpenProject(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Projects", _uiVals.ProjectName, $"{_uiVals.ProjectName}.slproj"),
                config, s => s, _logger, _tracker);
            _logger.Log($"Project loaded from '{_project.ProjectFile}'");
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            TestContext.AddTestAttachment(_logger.LogFile);
            _driver.Quit();
            string artifactsAssetsDir = Path.Combine(_uiVals!.ArtifactsDir, "assets");
            if (Directory.Exists(artifactsAssetsDir))
            {
                Directory.Delete(artifactsAssetsDir, true);
            }
            Directory.CreateDirectory(artifactsAssetsDir);
            IO.CopyFiles(Path.Combine(_project!.BaseDirectory, "assets"), artifactsAssetsDir, _logger, recurse: true);
            Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Projects", _uiVals.ProjectName), true);
            string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Logs", "SerialLoops.log");
            _wad?.Kill();
            _wad?.Dispose();
            if (File.Exists(logFile))
            {
                File.Copy(logFile, Path.Combine(_uiVals.ArtifactsDir, "SerialLoops.log"), overwrite: true);
            }
            else
            {
                using FileStream fs = File.Create(Path.Combine(_uiVals.ArtifactsDir, "SerialLoops.log"));
                using StreamWriter sw = new(fs);
                sw.WriteLine("Log was not created");
            }
        }

        [TearDown]
        public void AfterTest()
        {
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
        }

        [Test]
        public void CanOpenAboutDialogTwice()
        {
            for (int i = 0; i < 2; i++)
            {
                _logger.Log($"Attempting to open about dialog time {i + 1}...");
                _driver.FindElementByName("Help").Click();
                Thread.Sleep(200);
                _driver.FindElementByName("About...").Click();
                Thread.Sleep(200);
                _driver.FindElementByName("About").FindElementByName("Close").Click();
                Thread.Sleep(200);
            }
        }

        private readonly static string[] HacksToTest = ["Skip OP", "Change OP_MODE Chibi"];
        [Test, TestCaseSource(nameof(HacksToTest))]
        public void TestAsmHackApplicationAndReversion(string hackToApply)
        {
            string testArtifactsFolder = CommonHelpers.CreateTestArtifactsFolder(TestContext.CurrentContext.Test.Name, _uiVals!.ArtifactsDir);

            if (!(_project?.Config.UseDocker ?? true))
            {
                _driver.FindElementByName("File").Click();
                _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, "file_menu.png"));
                _driver.FindElementByName("Preferences...").Click();
                Actions actions = new(_driver);
                actions.MoveToElement(_driver.FindElementByName("Use Docker for ASM Hacks"));
                actions.Build().Perform();
                _driver.FindElementByClassName("CheckBox").Click();
                _driver.FindElementByName("Save").Click();
            }

            _logger.Log("Applying hack...");
            _driver.FindElementByName("Tools").Click();
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, "tools_clicked.png"));
            _driver.FindElementByName("Apply Hacks...").Click();
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, "available_hacks.png"));
            _driver.FindElementByName(hackToApply).Click();
            _driver.FindElementByName("Save").Click();
            Thread.Sleep(TimeSpan.FromSeconds(10)); // Allow time for hacks to be assembled
            if (Helpers.OnWindows11())
            {
                // Dialogs count as windows on Win11 but are part of the same window on Win10, from basic testing
                _driver.SwitchToWindowWithName("Successfully applied hacks!", "Success!", "Error");
            }
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, "hack_apply_result_dialog.png"));
            _driver.FindElementByName("OK").Click();
            Thread.Sleep(TimeSpan.FromSeconds(1)); // Allow time to clean up the containers
            List<AsmHack> hacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine("Sources", "hacks.json"))) ?? [];
            Assert.That(hacks.First(h => h.Name == hackToApply).Applied(_project), Is.True);

            _logger.Log("Reverting hack...");
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            Thread.Sleep(100);
            _driver.FindElementByName("Tools").Click();
            Thread.Sleep(100);
            _driver.FindElementByName("Apply Hacks...").Click();
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            _driver.FindElementByName(hackToApply).Click();
            _driver.FindElementByName("Save").Click();
            Thread.Sleep(TimeSpan.FromSeconds(3)); // Allow time for hacks to be disabled
            if (Helpers.OnWindows11())
            {
                // Dialogs count as windows on Win11 but are part of the same window on Win10, from basic testing
                _driver.SwitchToWindowWithName("Successfully applied hacks!", "Success!", "Error");
            }
            _driver.FindElementByName("OK").Click();
            Thread.Sleep(TimeSpan.FromSeconds(1)); // Allow time to clean up the containers
            Assert.That(hacks.First(h => h.Name == hackToApply).Applied(_project), Is.False);
        }

        // Add KBG00 (KINETIC_SCREEN) to this as part of https://github.com/haroohie-club/SerialLoops/issues/225
        // TEX_BG, TEX_CG, TEX_CG_DUAL, TEX_CG_WIDE, TEX_CG_SINGLE
        private readonly static object[][] BackgroundsToTest = [["BG_BRIDGE_DAY", 0x027, 0x028], ["BG_EV020", 0x2CD, 0x2CE], ["BG_EV150_D", 0x317, 0x318], ["BG_EV550", 0x3F3, 0x3F4], ["BG_EV060", 0x2EC, -1]];
        [Test, TestCaseSource(nameof(BackgroundsToTest))]
        public void TestBackgrounds_ExtractReplaceCropScale(string bgName, int bgIdx1, int bgIdx2)
        {
            string testArtifactsFolder = CommonHelpers.CreateTestArtifactsFolder(TestContext.CurrentContext.Test.Name, _uiVals!.ArtifactsDir);

            _driver.OpenItem(bgName);
            Thread.Sleep(100);
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, $"{bgName}_openTab.png"));

            _driver.FindElementByName("Export").Click();
            string exportedImagePath = Path.Combine(testArtifactsFolder, $"{bgName}.png");
            _driver.HandleFileDialog(exportedImagePath);
            Thread.Sleep(500);
            Assert.That(ImageTool.GetPercentageDifference(Path.Combine(_testAssets, $"{bgName}.png"), exportedImagePath), Is.LessThanOrEqualTo(1));
            TestContext.AddTestAttachment(exportedImagePath);

            _driver.FindElementByName("Replace").Click();
            Thread.Sleep(200);
            _driver.HandleFileDialog(Path.Combine(_testAssets, TEST_BG_ASSET));

            _driver.SwitchToWindowWithName("Crop & Scale");
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, $"{bgName}_nocropnoscale.png"));
            Thread.Sleep(200);
            WindowsElement widthStepperTextField = _driver.FindElementByAccessibilityId("PART_TextBox");
            Actions actions = new(_driver);
            actions.DoubleClick(widthStepperTextField);
            actions.SendKeys("750");
            actions.Build().Perform();
            Thread.Sleep(200);
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, $"{bgName}_scale.png"));

            WindowsElement image = _driver.FindElementByClassName("Image");
            actions = new(_driver);
            actions.ClickAndHold(image);
            actions.MoveByOffset(300, 60);
            actions.Release();
            actions.Build().Perform();
            Thread.Sleep(200);
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, $"{bgName}_moved.png"));

            _driver.FindElementByName("Save").Click();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            _driver.SwitchToWindowWithName($"Serial Loops - {_uiVals.ProjectName}");
            Thread.Sleep(500);
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, $"{bgName}_saved.png"));

            actions = new(_driver);
            actions.KeyDown(Keys.Control);
            actions.SendKeys("s");
            actions.KeyUp(Keys.Control);
            actions.Build().Perform();

            Assert.That(ImageTool.GetPercentageDifference(Path.Combine(_testAssets, $"{bgIdx1:X3}.png"), Path.Combine(_project!.BaseDirectory, "assets", "graphics", $"{bgIdx1:X3}.png")), Is.LessThanOrEqualTo(1));
            if (bgIdx2 > 0)
            {
                Assert.That(ImageTool.GetPercentageDifference(Path.Combine(_testAssets, $"{bgIdx2:X3}.png"), Path.Combine(_project!.BaseDirectory, "assets", "graphics", $"{bgIdx2:X3}.png")), Is.LessThanOrEqualTo(1));
            }

            _driver.CloseCurrentItem();
        }

        private readonly static object[][] BGMsToTest = [["BGM001 - Another Wonderful Day!", 0], ["BGM027 - You Can Do It!", 19]];
        [Test, TestCaseSource(nameof(BGMsToTest))]
        public void TestBGMs_ExtractReplaceRestore(string bgmName, int bgmIndex)
        {
            string testArtifactsFolder = CommonHelpers.CreateTestArtifactsFolder(TestContext.CurrentContext.Test.Name, _uiVals!.ArtifactsDir);

            _driver.OpenItem(bgmName);
            Thread.Sleep(100);
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, $"{bgmName}_openTab.png"));

            _driver.FindElementByName("Extract").Click();
            Thread.Sleep(200);
            string extractedWavPath = Path.Combine(testArtifactsFolder, $"{bgmName.Split(' ')[0]}.wav");
            _driver.HandleFileDialog(extractedWavPath);
            Thread.Sleep(TimeSpan.FromSeconds(5)); // Wait for extraction
            Assert.That(File.ReadAllBytes(extractedWavPath), Is.Not.Empty);
            _driver.FindElementByName("Replace").Click();
            Thread.Sleep(200);
            _driver.HandleFileDialog(extractedWavPath);
            Thread.Sleep(TimeSpan.FromSeconds(5)); // Wait for replacement
            _driver.FindElementByName("Restore").Click();
            Thread.Sleep(200);
            string dupeExtractedWavPath = Path.Combine(testArtifactsFolder, $"{bgmName.Split(' ')[0]}-dupe.wav");
            _driver.FindElementByName("Extract").Click();
            Thread.Sleep(200);
            _driver.HandleFileDialog(dupeExtractedWavPath);
            Thread.Sleep(TimeSpan.FromSeconds(5)); // Wait for extraction

            byte[] originalmd5 = MD5.HashData(File.ReadAllBytes(extractedWavPath));
            byte[] dupemd5 = MD5.HashData(File.ReadAllBytes(dupeExtractedWavPath));
            Assert.That(originalmd5, Is.EquivalentTo(dupemd5));

            _driver.CloseCurrentItem();
        }

        [Test, TestCaseSource(nameof(BGMsToTest))]
        public void TestBGMs_SoundPlaying(string bgmName, int bgmIndex)
        {
            _driver.OpenItem(bgmName);
            Thread.Sleep(100);

            WasapiLoopbackCapture loopback = new();
            bool soundPlaying = false;
            loopback.DataAvailable += (sender, args) =>
            {
                if (args.Buffer.Any(b => b != 0))
                {
                    soundPlaying = true;
                }
            };
            WindowsElement playButton = _driver.FindElementsByClassName("Button")[1];
            WindowsElement stopButton = _driver.FindElementsByClassName("Button")[2];
            loopback.StartRecording();
            playButton.Click();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.That(soundPlaying, Is.True);
            playButton.Click();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            soundPlaying = false;
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.That(soundPlaying, Is.False);
            playButton.Click();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.That(soundPlaying, Is.True);
            stopButton.Click();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            soundPlaying = false;
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.That(soundPlaying, Is.False);
            playButton.Click();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.That(soundPlaying, Is.True);
            _driver.CloseCurrentItem();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            soundPlaying = false;
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.That(soundPlaying, Is.False);
            _driver.OpenItem(bgmName);
            _driver.FindElementsByClassName("Button")[1].Click();
            _driver.OpenItem((string)BGMsToTest.First(b => (string)b[0] != bgmName)[0]);
            WindowsElement window = _driver.FindElementByClassName("Window");
            Actions actions = new(_driver);
            actions.MoveToElement(window);
            actions.ContextClick();
            actions.Build().Perform();
            _driver.FindElementByName("Close All").Click();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            soundPlaying = false;
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.That(soundPlaying, Is.False);
            loopback.StopRecording();
        }
    }
}