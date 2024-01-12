using HaruhiChokuretsuLib.Util;
using System;
using System.IO;

namespace SerialLoops.UITests.Shared
{
    public class UiTestLogger(string logFile) : ILogger
    {
        public readonly string LogFile = logFile;

        public void Log(string message)
        {
            using FileStream fs = File.OpenWrite(LogFile);
            using StreamWriter sw = new(fs);
            sw.WriteLine(message);
        }

        public void LogError(string message, bool lookForWarnings = false)
        {
            using FileStream fs = File.OpenWrite(LogFile);
            using StreamWriter sw = new(fs);
            sw.WriteLine($"ERROR: {message}");
        }

        public void LogException(string message, Exception exception)
        {
            LogError($"{message}: {exception.Message}\n\n{exception.StackTrace}");
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            using FileStream fs = File.OpenWrite(LogFile);
            using StreamWriter sw = new(fs);
            sw.WriteLine($"WARNING: {message}");
        }
    }
}
