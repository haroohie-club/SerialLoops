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
        public static void HandleFileDialog(this MacDriver driver, string fileLoc)
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
            AppiumElement menuBarItem = driver.FindElement(MobileBy.IosClassChain($"**/XCUIElementTypeMenuBarItem[`title=\"{menuBarItemTitle}\"`]"));
            if (driver.FindElements(MobileBy.IosClassChain("**/XCUIElementTypeMenuItem")).Count == 0)
            {
                menuBarItem.Click();
            }
            else
            {
                actions = new(driver);
                actions.MoveToElement(menuBarItem);
                actions.Build().Perform();
            }
        }

        public static void OpenItem(this MacDriver driver, string itemName)
        {
            AppiumElement searchField = driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeSearchField"));
            searchField.Click();
            searchField.SendKeys(itemName);
            AppiumElement item = driver.FindElement(MobileBy.IosNSPredicate($"value == \"{itemName}\""));
            Actions actions = new(driver);
            actions.DoubleClick(item);
            actions.Build().Perform();
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
