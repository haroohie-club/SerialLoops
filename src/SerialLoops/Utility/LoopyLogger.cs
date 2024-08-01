using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SerialLoops.Assets;
using SerialLoops.Lib;

namespace SerialLoops.Utility
{
    public class LoopyLogger : ILogger
    {
        private Config _config;
        private StreamWriter _writer;
        private Window _owner;

        public LoopyLogger(Window window)
        {
            _owner = window;
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
            // Attempting to await this using the normal methods for awaiting in a synchronous context seems to deadlock the process, so we don't do it!!
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            LogErrorAsync(message, lookForWarnings);
#pragma warning restore CS4014
        }

        private async Task LogErrorAsync(string message, bool lookForWarnings = false)
        {
            await MessageBoxManager.GetMessageBoxStandard(Strings.Error, string.Format(Strings.ERROR___0_, message), ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterScreen).ShowWindowDialogAsync(_owner);
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
