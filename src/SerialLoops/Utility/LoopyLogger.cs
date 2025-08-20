using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using SerialLoops.Assets;
using SerialLoops.Lib;

namespace SerialLoops.Utility;

public class LoopyLogger : ILogger
{
    private readonly Window _owner;
    private ConfigUser _configUser;
    private string _logFile;

    /// <summary>
    /// WARNING: Do not use this method. It is only here for mocking
    /// </summary>
    public LoopyLogger()
    {
    }

    public LoopyLogger(Window window)
    {
        _owner = window;
    }

    public void Initialize(ConfigUser configUser)
    {
        _configUser = configUser;
        if (!Directory.Exists(_configUser.LogsDirectory))
        {
            Directory.CreateDirectory(_configUser.LogsDirectory);
        }
        _logFile = Path.Combine(_configUser.LogsDirectory, "SerialLoops.log");
    }

    private static string Stamp => $"\n({Environment.ProcessId}) {DateTimeOffset.Now} - ";
    public static string CrashLogLocation => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SerialLoops", "sl_crash.log");

    public virtual void Log(string message)
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
        Dispatcher.UIThread.Invoke(() => LogErrorAsync(message, lookForWarnings));
    }

    private async Task LogErrorAsync(string message, bool lookForWarnings = false)
    {
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
        await _owner.ShowMessageBoxAsync(Strings.Error, string.Format(Strings.LoggerErrorMessage, message), ButtonEnum.Ok, Icon.Error, this);
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
        string crashLog = $"{Stamp}SERIAL LOOPS CRASH: {ex.Message}\n\n{ex.StackTrace}";
        Console.WriteLine(crashLog);
        if (!Directory.Exists(Path.GetDirectoryName(CrashLogLocation)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogLocation)!);
        }
        File.AppendAllText(CrashLogLocation, crashLog);
    }

    public string ReadLog()
    {
        return File.ReadAllText(_logFile);
    }
}
