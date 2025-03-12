using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace SerialLoops.Controls;

public partial class LinkButton : UserControl
{
    public static readonly AvaloniaProperty<ICommand> CommandProperty = AvaloniaProperty.Register<LinkButton, ICommand>(nameof(Command));
    public static readonly AvaloniaProperty<string> TextProperty = AvaloniaProperty.Register<LinkButton, string>(nameof(Text));

    public string Icon
    {
        get => IconPath.Path;
        set
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IconPath.Path = string.IsNullOrEmpty(value)
                    ? null
                    : $"avares://SerialLoops/Assets/Icons/{value}.svg";
                IconPath.IsVisible = !string.IsNullOrEmpty(value);
            });
        }
    }
    public string Text
    {
        get => this.GetValue<string>(TextProperty);
        set
        {
            SetValue(TextProperty, value);
        }
    }

    public ICommand Command
    {
        get => this.GetValue<ICommand>(CommandProperty);
        set
        {
            SetValue(CommandProperty, value);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            LinkText.Text = Text;
        }
        else if (change.Property == CommandProperty)
        {
            BackingButton.Command = Command;
        }
    }

    public LinkButton()
    {
        InitializeComponent();
    }
}
