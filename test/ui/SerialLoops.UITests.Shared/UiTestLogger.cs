using HaruhiChokuretsuLib.Util;
using System;
using System.IO;

namespace SerialLoops.UITests.Shared
{
    public class UiTestLogger(TextWriter log) : ILogger
    {
        private TextWriter _log = log;

        public void Log(string message)
        {
            _log.WriteLine(message);
        }

        public void LogError(string message, bool lookForWarnings = false)
        {
            _log.WriteLine($"ERROR: {message}");
        }

        public void LogException(string message, Exception exception)
        {
            LogError($"{message}: {exception.Message}\n\n{exception.StackTrace}");
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            _log.WriteLine($"WARNING: {message}");
        }
    }
}
