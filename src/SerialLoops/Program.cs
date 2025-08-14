using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Utility;

namespace SerialLoops;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetAssembly(typeof(NAudio.Sdl2.WaveInSdl))!, DllImportResolver);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LoopyLogger logger = new(null);
            logger.LogCrash(ex);
            return 1;
        }

        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .WithInterFont()
            .LogToTrace();

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.Equals("SDL2") &&
            (Environment.GetEnvironmentVariable(EnvironmentVariables.Sandboxed) ?? bool.FalseString).Equals(
                bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            return NativeLibrary.Load("SDL2-2.0", assembly, searchPath);
        }

        return IntPtr.Zero;
    }
}
