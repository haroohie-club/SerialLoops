﻿using System;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.ReactiveUI;
using SerialLoops.Utility;

namespace SerialLoops;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // if (OperatingSystem.IsMacOS())
            // {
            //     Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            // }
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LoopyLogger logger = new(null);
            logger.LogCrash(ex);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .WithInterFont()
            .LogToTrace();
}
