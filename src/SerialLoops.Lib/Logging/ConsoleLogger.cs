using HaruhiChokuretsuLib.Util;
using System;

namespace SerialLoops.Lib.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void LogError(string message, bool lookForWarnings = false)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (lookForWarnings && message.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    LogWarning(message);
                    return;
                }
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"ERROR: {message}");
                Console.ForegroundColor = oldColor;
            }
            else
            {
                Console.WriteLine();
            }
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            {
                if (!string.IsNullOrEmpty(message))
                {
                    if (lookForErrors && message.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        LogError(message);
                        return;
                    }
                    ConsoleColor oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"WARNING: {message}");
                    Console.ForegroundColor = oldColor;
                }
                else
                {
                    Console.WriteLine();
                }
            }
        }
    }
}
