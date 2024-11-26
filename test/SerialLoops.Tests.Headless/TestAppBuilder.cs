using Avalonia;
using Avalonia.Headless;
using Avalonia.ReactiveUI;

[assembly: AvaloniaTestApplication(typeof(SerialLoops.Tests.Headless.TestAppBuilder))]
namespace SerialLoops.Tests.Headless;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseReactiveUI()
        .UseHeadless(new()
        {
            UseHeadlessDrawing = false,
        });
}
