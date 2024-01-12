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
        private readonly UiTestLogger _logger = new(Environment.GetEnvironmentVariable("LOG_FILE") ?? "testlog_console.log");
        private readonly ConsoleProgressTracker _tracker = new();
        private Process? _wad;

        [OneTimeSetUp]
        public void Setup()
        {
            // To run these tests locally, you can create a file called 'ui_vals.json' and place it next to the test assembly (in the output folder)
            // These tests assume default config for Serial Loops i.e. fresh install; some tests may fail if default config is not used

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
            _logger.Log(JsonSerializer.Serialize<UiVals>(_uiVals, new JsonSerializerOptions { WriteIndented = true }));

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

            _driver.LaunchApp();

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
            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            } while (_driver.WindowHandles.Count > 1);
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
            TestContext.AddTestAttachment(_logger.LogFile);
            _driver.Quit();
            Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "Projects", _uiVals!.ProjectName), true);
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

        [Test]
        public void TestAsmHackApplication()
        {
            string hackToApply = "Skip OP";
            _driver.FindElementByName("Tools").Click();
            _driver.FindElementByName("Apply Hacks...").Click();
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            _driver.FindElementByName(hackToApply).Click();
            _driver.FindElementByName("Save").Click();
            Thread.Sleep(TimeSpan.FromSeconds(15));
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            Actions actions = new(_driver);
            actions.SendKeys(Keys.Enter);
            actions.Build().Perform();
            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            _driver.FindElementByName("Close").Click();
            List<AsmHack> hacks = JsonSerializer.Deserialize<List<AsmHack>>(File.ReadAllText(Path.Combine("Sources", "hacks.json"))) ?? [];
            Assert.That(hacks.First(h => h.Name == hackToApply).Applied(_project));
        }
    }
}