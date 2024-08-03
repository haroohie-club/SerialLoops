using System;
using System.IO;
using System.Threading;
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
        private readonly Window _owner;
        private Config _config;
        private string _logFile;

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
            _logFile = Path.Combine(_config.LogsDirectory, $"SerialLoops.log");
        }

        private static string Stamp => $"\n({Environment.ProcessId}) {DateTimeOffset.Now} - ";

        public void Log(string message)
        {
            if (!string.IsNullOrEmpty(_logFile) && !string.IsNullOrEmpty(message))
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        File.AppendAllText(_logFile, $"{Stamp}{message}");
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }
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
            await _owner.ShowMessageBoxAsync(Strings.Error, string.Format(Strings.ERROR___0_, message), ButtonEnum.Ok, Icon.Error, this);
            if (!string.IsNullOrEmpty(_logFile) && !string.IsNullOrEmpty(message))
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        File.AppendAllText(_logFile, $"{Stamp}ERROR: {message}");
                        break;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                }
            }
        }

        public void LogException(string message, Exception exception)
        {
            LogError($"{message}\n{exception.Message}");
            LogWarning($"\n{exception.StackTrace}");
        }

        public void LogWarning(string message, bool lookForErrors = false)
        {
            if (!string.IsNullOrEmpty(_logFile) && !string.IsNullOrEmpty(message))
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        File.AppendAllText(_logFile, $"{Stamp}WARNING: {message}");
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        public void LogCrash(Exception ex)
        {
            if (!string.IsNullOrEmpty(_logFile))
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        File.AppendAllText(_logFile, $"{Stamp}SERIAL LOOPS CRASH: {ex.Message}\n\n{ex.StackTrace}");
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }
    }
}
