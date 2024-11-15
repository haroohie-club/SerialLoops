using Avalonia;
using Avalonia.Markup.Xaml;

namespace SerialLoops.Tests.Headless;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
