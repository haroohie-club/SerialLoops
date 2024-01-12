using OpenQA.Selenium.Appium.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
    }
}
