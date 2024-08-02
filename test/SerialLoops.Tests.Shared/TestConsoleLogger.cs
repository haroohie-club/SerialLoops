using System;
using HaruhiChokuretsuLib.Util;
using NUnit.Framework;

namespace SerialLoops.Tests.Shared
{
    public class TestConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            TestContext.Out.WriteLine(message);
        }
        public void LogError(string message, bool lookForWarnings = false)
        {
            TestContext.Error.WriteLine(message);
        }
        public void LogException(string message, Exception exception)
        {
            TestContext.Error.WriteLine($"{message} ({exception.Message})\n\n{exception.StackTrace}");
        }
        public void LogWarning(string message, bool lookForErrors = false)
        {
            TestContext.Out.WriteLine($"WARNING: {message}");
        }
    }
}
