using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(SerialLoops.Tests.Headless.TestAppBuilder))]
namespace SerialLoops.Tests.Headless
{
    public class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions()
            {
                UseHeadlessDrawing = false,
            });
    }
}
