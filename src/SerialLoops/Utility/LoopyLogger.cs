using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using System;
using System.IO;

namespace SerialLoops.Utility
{
    public class LoopyLogger : ILogger
    {
        public static string LogLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        private StreamWriter _writer;

        public LoopyLogger()
        {
            if (!Directory.Exists(LogLocation))
            {
                Directory.CreateDirectory(LogLocation);
            }

            _writer = File.AppendText(Path.Combine(LogLocation, $"SerialLoops.log"));
        }

        public void Log(string message)
        {
            _writer.WriteLine($"{DateTimeOffset.Now} - {message}");
            _writer.Flush();
        }

        public void LogError(string message, bool lookForWarnings = false)
        {
            MessageBox.Show($"ERROR: {message}", MessageBoxType.Error);
            _writer.WriteLine($"{DateTimeOffset.Now} - ERROR: {message}");
            _writer.Flush();
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            _writer.WriteLine($"{DateTimeOffset.Now} - WARNING: {message}");
            _writer.Flush();
        }
    }
}
