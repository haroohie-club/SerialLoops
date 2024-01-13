using GotaSoundIO.IO;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Interactions;
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
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeStaticText[`value == \"New Project\"`]")).Click();
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeTextField[1]")).SendKeys(_uiVals.ProjectName);
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeButton[`title == \"Open ROM\"`]")).Click();
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
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeButton[`title == \"Create\"`]")).Click();
            while (_driver.FindElements(MobileBy.IosClassChain("**/XCUIElementTypeDialog")).Count >= 1)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            _driver.GetScreenshot().SaveAsFile(Path.Combine(_uiVals.ArtifactsDir, "loaded_project.png"));
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
                _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeMenuItem[`title=\"About...\"`]")).Click();
                Thread.Sleep(200);
                _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeDialog[`title=\"About\"`]/**/XCUIElementTypeButton[1]")).Click();
                Thread.Sleep(200);
                _driver.GetScreenshot().SaveAsFile(Path.Combine(_uiVals!.ArtifactsDir, $"about_dialog{i}.png"));
                TestContext.AddTestAttachment(Path.Combine(_uiVals!.ArtifactsDir, $"about_dialog{i}.png"));
            }
        }
    }
}