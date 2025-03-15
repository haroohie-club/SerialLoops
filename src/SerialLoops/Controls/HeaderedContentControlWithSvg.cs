using Avalonia;
using Avalonia.Controls.Primitives;

namespace SerialLoops.Controls;

public class HeaderedContentControlWithSvg : HeaderedContentControl
{
    public static readonly AvaloniaProperty<string> IconPathProperty = AvaloniaProperty.Register<HeaderedContentControlWithSvg, string>(nameof(IconPath));
    public static readonly AvaloniaProperty<string> IconTipProperty = AvaloniaProperty.Register<HeaderedContentControlWithSvg, string>(nameof(IconTip));

    public string IconPath
    {
        get => this.GetValue<string>(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }
    public string IconTip
    {
        get => this.GetValue<string>(IconTipProperty);
        set => SetValue(IconTipProperty, value);
    }
}
