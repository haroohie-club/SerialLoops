using HaruhiChokuretsuLib.Util;
using System;
using System.IO;

namespace SerialLoops.Tests.Shared
{
    public class UiTestLogger(string logFile) : ILogger
    {
        public readonly string LogFile = logFile;

        public void Log(string message)
        {
            File.AppendAllText(LogFile, $"{message}\n");
        }

        public void LogError(string message, bool lookForWarnings = false)
        {
            File.AppendAllText(LogFile, $"ERROR: {message}\n");
        }

        public void LogException(string message, Exception exception)
        {
            LogError($"{message}: {exception.Message}\n\n{exception.StackTrace}");
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            File.AppendAllText(LogFile, $"WARNING: {message}\n");
        }
    }
}
