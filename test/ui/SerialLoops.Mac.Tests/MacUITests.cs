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
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeButton[`title == \"Skip Update\"`]")).Click();
            _driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeStaticText[`value == \"New Project\"`]")).Click();
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeTextField[1]")).SendKeys(_uiVals.ProjectName);
            _driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeDialog/**/XCUIElementTypeButton[`title == \"Open ROM\"`]")).Click();
            _driver.FindElement(MobileBy.IosNSPredicate("label == \"open\"")).SendKeys($"{Keys.Command}{Keys.Shift}G/U");
            _driver.FindElement(MobileBy.IosNSPredicate("label == \"open\"")).SendKeys(_uiVals.RomLoc[1..]);
            _driver.FindElement(MobileBy.IosNSPredicate("label == \"open\"")).FindElement(MobileBy.IosClassChain("**/XCUIElementTypeSheet")).SendKeys(Keys.Return);
            Thread.Sleep(500);
            _driver.FindElement(MobileBy.IosNSPredicate("label == \"open\"")).SendKeys(Keys.Return);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        [OneTimeTearDown] 
        public void Teardown() 
        {
            _driver?.Quit();
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}