using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
using SerialLoops.Tests;
using SerialLoops.UITests.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace SerialLoops.Wpf.Tests
{
    public class WpfUITests
    {
        private WindowsDriver<WindowsElement> _driver;
        private UiVals? _uiVals;
        private Project? _project;
        private readonly ConsoleLogger _logger = new();
        private readonly ConsoleProgressTracker _tracker = new();

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
                    ArtifactsDir = Environment.GetEnvironmentVariable("BUILD_ARTIFACTSSTAGINGDIRECTORY") ?? "artifacts",
                };
            }

            if (!string.IsNullOrEmpty(_uiVals.WinAppDriverLoc))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _uiVals.WinAppDriverLoc
                });
            }

            Uri serverUri = new(Environment.GetEnvironmentVariable("APPIUM_HOST") ?? "http://127.0.0.1:4723/");
            
            AppiumOptions driverOptions = new();
            driverOptions.AddAdditionalCapability("app", _uiVals!.AppLoc);
            driverOptions.AddAdditionalCapability("deviceName", "WindowsPC");

            _driver = new(serverUri, driverOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1.5);

            Thread.Sleep(TimeSpan.FromSeconds(3));
            _driver.SwitchTo().Window(_driver.WindowHandles.First()); // Switch to the update available dialog
            _driver.FindElementByName("Skip Update").Click(); // close the dialog

            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            _driver.GetScreenshot().SaveAsFile(Path.Combine(_uiVals.ArtifactsDir, "start.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals.ArtifactsDir, "start.png"), "Initial screen after project creation");
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
            Thread.Sleep(TimeSpan.FromSeconds(20));
            _driver.GetScreenshot().SaveAsFile(Path.Combine(_uiVals.ArtifactsDir, "project_open.png"));
            TestContext.AddTestAttachment(Path.Combine(_uiVals.ArtifactsDir, "project_open.png"), "Initial screen after project creation");

            _driver.FindElementByName("File").Click();
            _driver.FindElementByName("Preferences...").Click();
            actions = new(_driver);
            actions.MoveToElement(_driver.FindElementByName("Use Docker for ASM Hacks"));
            actions.Build().Perform();
            _driver.FindElementByClassName("CheckBox").Click();
            _driver.FindElementByName("Save").Click();

            (_project, _) = Project.OpenProject(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Projects", _uiVals.ProjectName, $"{_uiVals.ProjectName}.slproj"),
                Config.LoadConfig(_logger), _logger, _tracker);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            _driver.Quit();
            Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Projects", _uiVals!.ProjectName), true);
            File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Logs", "SerialLoops.log"), Path.Combine(_uiVals.ArtifactsDir, "SerialLoops.log"), overwrite: true);
        }

        [Test]
        public void TestAsmHackApplication()
        {
            _driver.FindElementByName("Tools").Click();
            _driver.FindElementByName("Apply Hacks...").Click();
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            _driver.FindElementByName("Skip OP").Click();
            _driver.FindElementByName("Save").Click();
            Thread.Sleep(TimeSpan.FromSeconds(15));
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            Actions actions = new(_driver);
            actions.SendKeys(Keys.Enter);
            actions.Build().Perform();
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            _driver.FindElementByName("Close").Click();
            List<AsmHack> hacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine("Sources", "hacks.json"))) ?? [];
            Assert.That(hacks[0].Applied(_project));
        }
    }
}