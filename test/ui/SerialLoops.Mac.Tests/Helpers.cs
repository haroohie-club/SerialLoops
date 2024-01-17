using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.ImageComparison;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Interactions;
using SerialLoops.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SerialLoops.Mac.Tests
{
    public static class Helpers
    {
        public static void GetAndSaveScreenshot(this MacDriver driver, string screenshotLocation)
        {
            driver.GetScreenshot().SaveAsFile(screenshotLocation);
            TestContext.AddTestAttachment(screenshotLocation);
        }

        public static void HandleOpenFileDialog(this MacDriver driver, string fileLoc)
        {
            AppiumElement openFileDialog = driver.FindElement(MobileBy.IosNSPredicate("label == \"open\""));
            openFileDialog.SendKeys($"{Keys.Command}{Keys.Shift}g/");
            openFileDialog.SendKeys(fileLoc[1..]);
            AppiumElement fileField = driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeTextField[`value == \"{fileLoc}\"`]"));
            driver.ExecuteScript("macos: doubleClick", new Dictionary<string, object>
            {
                { "x", fileField.Location.X + 30 },
                { "y", fileField.Location.Y + 60 },
            });
            Thread.Sleep(500);
            driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeSheet[`label == \"open\"`]/**/XCUIElementTypeButton[`title == \"Open\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        private static readonly string[] enterSequence = ["XCUIKeyboardKeyEnter"];
        public static void HandleSaveFileDialog(this MacDriver driver, string fileLoc)
        {
            driver.ExecuteScript("macos: keys", new Dictionary<string, object>
            {
                { "keys", fileLoc.Select(c => $"{c}").ToArray() },
            });
            Thread.Sleep(200);
            driver.ExecuteScript("macos: keys", new Dictionary<string, object>
            {
                { "keys", enterSequence },
            });
            Thread.Sleep(500);
            driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeSheet[`label == \"save\"`]/**/XCUIElementTypeButton[`title == \"Save\"`]")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        public static SimilarityMatchingResult GetImagesSimilarity(this MacDriver driver, string assetDir, string assetName, string compareFile, bool visualize = true)
        {
            string originalBase64 = TestAssetsDownloader.GetAssetBase64(assetDir, assetName);
            string compareBase64 = Convert.ToBase64String(File.ReadAllBytes(compareFile));
            return driver.GetImagesSimilarity(originalBase64, compareBase64, new() { Visualize = true });
        }

        public static void OpenMenu(this MacDriver driver, string menuBarItemTitle)
        {
            // handles popping the menu when we're in full screen mode
            Actions actions = new(driver);
            actions.MoveToLocation(0, 0);
            actions.Build().Perform();
            Thread.Sleep(200);
            driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeMenuBarItem[`title=\"{menuBarItemTitle}\"`]")).Click();
        }

        public static void OpenItem(this MacDriver driver, string itemName, string artifactsDir)
        {
            AppiumElement searchField = driver.FindElements(MobileBy.IosClassChain("**/XCUIElementTypeSearchField")).Last(); // the built-in search field is [1], so we need to access the second (ours)
            searchField.Click();
            searchField.SendKeys(itemName);
            driver.GetAndSaveScreenshot(Path.Combine(artifactsDir, $"{itemName}_search.png"));
            AppiumElement item = driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeStaticText[`value == \"{itemName}\"`]"));
            driver.ExecuteScript("macos: doubleClick", new Dictionary<string, object>
            {
                { "elementId", item.Id },
            });
            driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeSearchField/**/XCUIElementTypeButton[`label == \"cancel\"`]")).Click();
        }

        public static void CloseCurrentItem(this MacDriver driver)
        {
            // Editor context menus on Mac are currently broken. Implement this once
            // https://github.com/haroohie-club/SerialLoops/issues/326 is fixed
            // In the meantime, it's actually not a huge deal for tests if other tabs are open
        }
    }
}
