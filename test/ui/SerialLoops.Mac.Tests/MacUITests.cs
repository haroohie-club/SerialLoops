using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Support.Extensions;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
using SerialLoops.Tests.Shared;
using SerialLoops.UITests.Shared;
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
                string romUri = Environment.GetEnvironmentVariable(UiVals.ROM_URI_ENV_VAR) ?? string.Empty;
                string romPath = Path.Combine(Directory.GetCurrentDirectory(), UiVals.ROM_NAME);
                HttpClient httpClient = new();
                using Stream downloadStream = httpClient.Send(new() { Method = HttpMethod.Get, RequestUri = new(romUri) }).Content.ReadAsStream();
                using FileStream fileStream = new(romPath, FileMode.Create);
                downloadStream.CopyTo(fileStream);
                fileStream.Flush();

                _uiVals = new()
                {
                    AppLoc = Environment.GetEnvironmentVariable(UiVals.APP_LOCATION_ENV_VAR) ?? string.Empty,
                    ProjectName = Environment.GetEnvironmentVariable(UiVals.PROJECT_NAME_ENV_VAR) ?? "MacUITest",
                    RomLoc = romPath,
                    ArtifactsDir = Environment.GetEnvironmentVariable(UiVals.ARTIFACTS_DIR_ENV_VAR) ?? "artifacts",
                };
            }
            _logger.Log(JsonSerializer.Serialize(_uiVals, new JsonSerializerOptions { WriteIndented = true }));

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
            _driver.FindElement(MobileBy.IosClassChain($"XCUIElementTypeDialog/**/XCUIElementTypeButton[`title == \"{UiVals.SKIP_UPDATE}\"`]")).Click();
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeStaticText[`value == \"{UiVals.NEW_PROJECT}\"`]")).Click();
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeTextField[1]")).SendKeys(_uiVals.ProjectName);
            _driver.FindElement(MobileBy.IosClassChain($"XCUIElementTypeDialog/**/XCUIElementTypeButton[`title == \"{UiVals.OPEN_ROM}\"`]")).Click();
            AppiumElement openFileDialog = _driver.FindElement(MobileBy.IosNSPredicate("label == \"open\""));
            openFileDialog.SendKeys($"{Keys.Command}{Keys.Shift}g/");
            openFileDialog.SendKeys(_uiVals.RomLoc[1..]);
            AppiumElement fileField = _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeTextField[`value == \"{_uiVals.RomLoc}\"`]"));
            _driver.ExecuteScript("macos: doubleClick", new Dictionary<string, object>
            {
                { "x", fileField.Location.X + 30 },
                { "y", fileField.Location.Y + 60 },
            });
            Thread.Sleep(500);
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeSheet[`label == \"open\"`]/**/XCUIElementTypeButton[`title == \"Open\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeButton[`title == \"{UiVals.CREATE}\"`]")).Click();
            while (_driver.FindElements(MobileBy.IosClassChain("**/XCUIElementTypeDialog")).Count >= 1)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            _driver.GetScreenshot().SaveAsFile(Path.Combine(_uiVals.ArtifactsDir, "loaded_project.png"));

            _logger.Log("Loading project...");
            string configPath = Path.Combine(Path.GetDirectoryName(_uiVals.AppLoc) ?? string.Empty, "Contents", "MacOS", "config.json");
            Config config = File.Exists(configPath) ? (JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath)) ?? Config.LoadConfig(_logger)) : Config.LoadConfig(_logger);
            (_project, _) = Project.OpenProject(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Projects", _uiVals.ProjectName, $"{_uiVals.ProjectName}.slproj"),
                config, s => s, _logger, _tracker);
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
                _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeMenuBarItem[`title=\"SerialLoops\"`]")).Click();
                Thread.Sleep(200);
                _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeMenuItem[`title=\"{UiVals.ABOUT_ELLIPSIS}\"`]")).Click();
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
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeMenuBarItem[`title=\"{UiVals.TOOLS}\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "tools_clicked.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals.ArtifactsDir, "tools_clicked.png"), "The app after the tools menu was clicked but before clicking Apply Hacks");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeMenuItem[`title=\"{UiVals.APPLY_HACKS}\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "available_hacks.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, "available_hacks.png"), "The available hacks dialog");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeCheckBox[`title=\"{hackToApply}\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, $"applying_hack_{hackToApply.Replace(' ', '_')}.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, $"applying_hack_{hackToApply.Replace(' ', '_')}.png"), $"Applying the hack {hackToApply}");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeDialog[`title=\"Apply Assembly Hacks\"`]/**/XCUIElementTypeButton[`title=\"{UiVals.SAVE}\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(10)); // Allow time for hacks to be assembled
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "hack_apply_result_dialog.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, "hack_apply_result_dialog.png"), "The alert indicating whether the hack application succeeded or not");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeDialog[`label=\"alert\"`]/**/XCUIElementTypeButton[`title=\"{UiVals.OK}\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(1)); // Allow time to clean up the containers
            List<AsmHack> hacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine("Sources", "hacks.json"))) ?? [];
            Assert.That(hacks.First(h => h.Name == hackToApply).Applied(_project), Is.True);

            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeMenuBarItem[`title=\"{UiVals.TOOLS}\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "tools_clicked.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals.ArtifactsDir, "tools_clicked.png"), "The app after the tools menu was clicked but before clicking Apply Hacks");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeMenuItem[`title=\"{UiVals.APPLY_HACKS}\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "available_hacks.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, "available_hacks.png"), "The available hacks dialog");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeCheckBox[`title=\"{hackToApply}\"`]")).Click();
            Thread.Sleep(200);
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, $"applying_hack_{hackToApply.Replace(' ', '_')}.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, $"applying_hack_{hackToApply.Replace(' ', '_')}.png"), $"Applying the hack {hackToApply}");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeDialog[`title=\"Apply Assembly Hacks\"`]/**/XCUIElementTypeButton[`title=\"{UiVals.SAVE}\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(5)); // Allow time for hacks to be reverted
            _driver.TakeScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, "hack_apply_result_dialog.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, "hack_apply_result_dialog.png"), "The alert indicating whether the hack application succeeded or not");
            _driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeDialog[`label=\"alert\"`]/**/XCUIElementTypeButton[`title=\"{UiVals.OK}\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(1)); // Allow time to clean up the containers
            Assert.That(hacks.First(h => h.Name == hackToApply).Applied(_project), Is.False);
        }
    }
}