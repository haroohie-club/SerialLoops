using HaruhiChokuretsuLib.Util;
using System;
using System.IO;

namespace SerialLoops.UITests.Shared
{
    public class UiTestLogger(string logFile) : ILogger
    {
        private string _logFile = logFile;

        public void Log(string message)
        {
            FileStream fs = File.OpenWrite(_logFile);
            StreamWriter sw = new(fs);
            sw.WriteLine(message);
        }

        public void LogError(string message, bool lookForWarnings = false)
        {
            FileStream fs = File.OpenWrite(_logFile);
            StreamWriter sw = new(fs);
            sw.WriteLine($"ERROR: {message}");
        }

        public void LogException(string message, Exception exception)
        {
            LogError($"{message}: {exception.Message}\n\n{exception.StackTrace}");
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            FileStream fs = File.OpenWrite(_logFile);
            StreamWriter sw = new(fs);
            sw.WriteLine($"WARNING: {message}");
        }
    }
}
