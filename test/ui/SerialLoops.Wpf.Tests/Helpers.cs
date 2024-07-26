using OpenQA.Selenium.Appium.Windows;
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

        public static bool OnWindows11()
        {
            // A reasonable enough approximation; it could be wrong if we're running on Server but we're not soooo
            return Environment.OSVersion.Version.Build >= 22000;
        }
    }
}
