using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.ImageComparison;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using System;
using System.Linq;

namespace SerialLoops.Wpf.Tests
{
    public static class Helpers
    {
        public static void SwitchToWindowWithName(this WindowsDriver<WindowsElement> driver, params string[] validTitles)
        {
            for (int i = 0; i < driver.WindowHandles.Count; i++)
            {
                driver.SwitchTo().Window(driver.WindowHandles[i]);
                if (validTitles.Any(t => t.Equals(driver.Title)))
                {
                    break;
                }
            }
        }

        public static void HandleFileDialog(this WindowsDriver<WindowsElement> driver, string file)
        {
            driver.FindElementByClassName("Edit").Click();
            Actions actions = new(driver);
            actions.SendKeys(file);
            actions.SendKeys(Keys.Enter);
            actions.Build().Perform();
        }

        public static void ExpandItemCategory(this WindowsDriver<WindowsElement> driver, string categoryName)
        {
            foreach (AppiumWebElement dataGridItem in driver.FindElementsByClassName("DataGridRow"))
            {
                var elements = dataGridItem.FindElementsByClassName("TextBlock");
                var texts = elements.Select(e => e.Text).ToList();
                if (elements.Any(e => e.Text.Contains(categoryName)))
                {
                    dataGridItem.FindElementByClassName("Button").Click();
                    break;
                }
            }
        }

        public static void OpenItem(this WindowsDriver<WindowsElement> driver, string itemName)
        {
            WindowsElement searchBox = driver.FindElementByClassName("TextBox");
            searchBox.Click();
            Actions typeAction = new(driver);
            typeAction.SendKeys(itemName);
            typeAction.Build().Perform();
            WindowsElement item = driver.FindElementByName(itemName);
            Actions doubleClickAction = new(driver);
            doubleClickAction.DoubleClick(item);
            doubleClickAction.Build().Perform();
            searchBox.FindElementByClassName("Button").Click();
        }

        public static void CloseCurrentItem(this WindowsDriver<WindowsElement> driver)
        {
            WindowsElement openTab = driver.FindElementByClassName("ScrollViewer");
            Actions actions = new(driver);
            actions.ContextClick(openTab);
            actions.Build().Perform();
            driver.FindElementsByName("Close").Last().Click();
        }

        public static bool OnWindows11()
        {
            // A reasonable enough approximation; it could be wrong if we're running on Server but we're not soooo
            return Environment.OSVersion.Version.Build >= 22000;
        }
    }
}
