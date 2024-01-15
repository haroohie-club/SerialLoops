using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.ImageComparison;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Support.Extensions;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
using SerialLoops.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace SerialLoops.Mac.Tests
{
    public class MacUITests
    {
        private MacDriver _driver;
        private UiVals? _uiVals;
        private Project? _project;
        private readonly UiTestLogger _logger = new(Environment.GetEnvironmentVariable("LOG_FILE") ?? "testlog_console.log");
        private readonly ConsoleProgressTracker _tracker = new();
        private string _testAssets = string.Empty;

        private const string TEST_BG_ASSET = "lizmelo-painting.png";


        [OneTimeSetUp]
        public void Setup()
        {
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
                    ProjectName = Environment.GetEnvironmentVariable("PROJECT_NAME") ?? "MacUITest",
                    RomLoc = romPath,
                    ArtifactsDir = Environment.GetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY") ?? "artifacts",
                };
            }
            _logger.Log(JsonSerializer.Serialize(_uiVals, new JsonSerializerOptions { WriteIndented = true }));

            _testAssets = TestAssetsDownloader.DownloadTestAssets().GetAwaiter().GetResult();

            AppiumOptions appiumOptions = new()
            {
                PlatformName = "mac",
                AutomationName = "mac2",
            };
            appiumOptions.AddAdditionalAppiumOption("bundleId", "club.haroohie.SerialLoops");
            appiumOptions.AddAdditionalAppiumOption("appPath", _uiVals.AppLoc);

            _driver = new(appiumOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1.5);

            Thread.Sleep(TimeSpan.FromSeconds(5));
            _driver.GetScreenshot().SaveAsFile(Path.Combine(_uiVals.ArtifactsDir, "start.png"));
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeButton[`title == \"Skip Update\"`]")).Click();
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeButton[2]")).Click(); // hit the full screen button so we have more room to work with
            Thread.Sleep(TimeSpan.FromSeconds(2));
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeStaticText[`value == \"New Project\"`]")).Click();
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeTextField[1]")).SendKeys(_uiVals.ProjectName);
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeButton[`title == \"Open ROM\"`]")).Click();
            _driver.HandleFileDialog(_uiVals.RomLoc);
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeButton[`title == \"Create\"`]")).Click();
            while (_driver.FindElements(MobileBy.IosClassChain("**/XCUIElementTypeDialog")).Count >= 1)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            _driver.GetScreenshot().SaveAsFile(Path.Combine(_uiVals.ArtifactsDir, "loaded_project.png"));

            _logger.Log("Loading project...");
            string configPath = Path.Combine(Path.GetDirectoryName(_uiVals.AppLoc) ?? string.Empty, "Contents", "MacOS", "config.json");
            Config config = File.Exists(configPath) ? (JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath)) ?? Config.LoadConfig(_logger)) : Config.LoadConfig(_logger);
            (_project, _) = Project.OpenProject(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Projects", _uiVals.ProjectName, $"{_uiVals.ProjectName}.slproj"),
                config, _logger, _tracker);
            _logger.Log($"Project loaded from '{_project.ProjectFile}'");
        }

        [OneTimeTearDown] 
        public void Teardown() 
        {
            _driver?.Quit();
            string projectDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Projects", _uiVals!.ProjectName);
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, true);
            }
            string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Logs", "SerialLoops.log");
            if (File.Exists(logFile))
            {
                File.Copy(logFile, Path.Combine(_uiVals.ArtifactsDir, "SerialLoops.log"), overwrite: true);
            }
        }

        [Test]
        public void CanOpenAboutDialogTwice()
        {
            for (int i = 0; i < 2; i++)
            {
                _driver.OpenMenu("SerialLoops");
                Thread.Sleep(200);
                _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeMenuItem[`title=\"About...\"`]")).Click();
                Thread.Sleep(200);
                _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeDialog[`title=\"About\"`]/**/XCUIElementTypeButton[1]")).Click();
                Thread.Sleep(200);
                _driver.GetScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, $"about_dialog{i}.png"));
                TestContext.AddTestAttachment(Path.Combine(_uiVals.ArtifactsDir, $"about_dialog{i}.png"));
            }
        }

        private readonly static string[] HacksToTest = ["Skip OP", "Change OP_MODE Chibi"];
        [Test, TestCaseSource(nameof(HacksToTest))]
        public void TestAsmHackApplicationAndReversion(string hackToApply)
        {
            _driver.OpenMenu("Tools");
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "tools_clicked.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals.ArtifactsDir, "tools_clicked.png"), "The app after the tools menu was clicked but before clicking Apply Hacks");
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeMenuItem[`title=\"Apply Hacks...\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "available_hacks.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, "available_hacks.png"), "The available hacks dialog");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeCheckBox[`title=\"{hackToApply}\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, $"applying_hack_{hackToApply.Replace(' ', '_')}.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, $"applying_hack_{hackToApply.Replace(' ', '_')}.png"), $"Applying the hack {hackToApply}");
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeDialog[`title=\"Apply Assembly Hacks\"`]/**/XCUIElementTypeButton[`title=\"Save\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(10)); // Allow time for hacks to be assembled
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "hack_apply_result_dialog.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, "hack_apply_result_dialog.png"), "The alert indicating whether the hack application succeeded or not");
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeDialog[`label=\"alert\"`]/**/XCUIElementTypeButton[`title=\"OK\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(1)); // Allow time to clean up the containers
            List<AsmHack> hacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine("Sources", "hacks.json"))) ?? [];
            Assert.That(hacks.First(h => h.Name == hackToApply).Applied(_project), Is.True);

            _driver.OpenMenu("Tools");
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "tools_clicked.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals.ArtifactsDir, "tools_clicked.png"), "The app after the tools menu was clicked but before clicking Apply Hacks");
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeMenuItem[`title=\"Apply Hacks...\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "available_hacks.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, "available_hacks.png"), "The available hacks dialog");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeCheckBox[`title=\"{hackToApply}\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, $"applying_hack_{hackToApply.Replace(' ', '_')}.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, $"applying_hack_{hackToApply.Replace(' ', '_')}.png"), $"Applying the hack {hackToApply}");
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeDialog[`title=\"Apply Assembly Hacks\"`]/**/XCUIElementTypeButton[`title=\"Save\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(5)); // Allow time for hacks to be reverted
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "hack_apply_result_dialog.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, "hack_apply_result_dialog.png"), "The alert indicating whether the hack application succeeded or not");
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeDialog[`label=\"alert\"`]/**/XCUIElementTypeButton[`title=\"OK\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(1)); // Allow time to clean up the containers
            Assert.That(hacks.First(h => h.Name == hackToApply).Applied(_project), Is.False);
        }

        // Add KBG00 (KINETIC_SCREEN) to this as part of https://github.com/haroohie-club/SerialLoops/issues/225
        // TEX_BG, TEX_CG, TEX_CG_DUAL, TEX_CG_WIDE, TEX_CG_SINGLE
        private readonly static object[][] BackgroundsToTest = [["BG_BRIDGE_DAY", 0x027, 0x028], ["BG_EV020", 0x2CD, 0x2CE], ["BG_EV150_D", 0x317, 0x318], ["BG_EV550", 0x3F3, 0x3F4], ["BG_EV060", 0x2EC, -1]];
        [Test, TestCaseSource(nameof(BackgroundsToTest))]
        public void TestEditBackgrounds(string bgName, int bgIdx1, int bgIdx2)
        {
            string testArtifactsFolder = Path.Combine(_uiVals!.ArtifactsDir, TestContext.CurrentContext.Test.Name.Replace(' ', '_').Replace(',', '_').Replace("\"", ""));
            if (Directory.Exists(testArtifactsFolder))
            {
                Directory.Delete(testArtifactsFolder, true);
            }
            Directory.CreateDirectory(testArtifactsFolder);

            _driver.OpenItem(bgName, _uiVals.ArtifactsDir);
            Thread.Sleep(100);
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, $"{bgName}_openTab.png"));
            
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeButton[`title == \"Export\"`]")).Click();
            string exportedImagePath = Path.Combine(testArtifactsFolder, $"{bgName}.png");
            _driver.HandleFileDialog(exportedImagePath);
            Thread.Sleep(500);
            SimilarityMatchingResult exportedImageMatch = _driver.GetImagesSimilarity(_testAssets, $"{bgName}.png", exportedImagePath);
            exportedImageMatch.SaveVisualizationAsFile(Path.Combine(testArtifactsFolder, "exported.png"));
            Assert.That(exportedImageMatch.Score, Is.GreaterThanOrEqualTo(0.99));

            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeButton[`title == \"Replace\"`]")).Click();
            Thread.Sleep(200);
            _driver.HandleFileDialog(Path.Combine(_testAssets, TEST_BG_ASSET));

            Thread.Sleep(500);
            _driver.GetAndSaveScreenshot(Path.Combine(testArtifactsFolder, $"{bgName}_nocropnoscale.png"));
            Thread.Sleep(200);
        }
    }
}