using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using System;
using System.IO;

namespace SerialLoops.Utility
{
    public class LoopyLogger : ILogger
    {
        private Config _config;
        private StreamWriter _writer;

        public LoopyLogger()
        {
        }

        public void Initialize(Config config)
        {
            _config = config;
            if (!Directory.Exists(_config.LogsDirectory))
            {
                Directory.CreateDirectory(_config.LogsDirectory);
            }
            _writer = File.AppendText(Path.Combine(_config.LogsDirectory, $"SerialLoops.log"));
        }

        public void Log(string message)
        {
            if (_writer is not null && !string.IsNullOrEmpty(message))
            {
                _writer.WriteLine($"{DateTimeOffset.Now} - {message}");
                _writer.Flush();
            }
        }

        public void LogError(string message, bool lookForWarnings = false)
        {
            Application.Instance.Invoke(() => MessageBox.Show($"ERROR: {message}", "Error", MessageBoxType.Error));
            if (_writer is not null && !string.IsNullOrEmpty(message))
            {
                _writer.WriteLine($"{DateTimeOffset.Now} - ERROR: {message}");
                _writer.Flush();
            }
        }

        public void LogException(string message, Exception exception)
        {
            LogError($"{message}\n{exception.Message}");
            LogWarning($"\n{exception.StackTrace}");
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            if (_writer is not null && !string.IsNullOrEmpty(message))
            {
                _writer.WriteLine($"{DateTimeOffset.Now} - WARNING: {message}");
                _writer.Flush();
            }
        }

        public void LogCrash(Exception ex)
        {
            if (_writer is not null)
            {
                _writer.WriteLine($"{DateTimeOffset.Now} - SERIAL LOOPS CRASH: {ex.Message}\n\n{ex.StackTrace}");
                _writer.Flush();
            }
        }
    }
}
