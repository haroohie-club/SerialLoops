using NUnit.Framework;
using System.IO;
using System.Net.Http;
using System;
using SerialLoops.UITests.Shared;
using System.Text.Json;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Mac;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;

namespace SerialLoops.Mac.Tests
{
    public class MacUITests
    {
        private MacDriver? _driver;
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
            appiumOptions.AddAdditionalAppiumOption("appPath", _uiVals.AppLoc);

            _driver = new(appiumOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1.5);
            _driver.ActivateApp(_uiVals.AppLoc);

            _driver.SwitchTo().Window(_driver.WindowHandles.First());
            Thread.Sleep(100); // Give it time
            _driver.FindElement(By.Name("Skip Update")).Click(); // close the dialog
        }

        [OneTimeTearDown] 
        public void Teardown() 
        {
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}