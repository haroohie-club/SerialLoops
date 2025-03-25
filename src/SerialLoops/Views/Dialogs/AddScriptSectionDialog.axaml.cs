using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SerialLoops.Views.Dialogs;

public partial class AddScriptSectionDialog : Window
{
    public AddScriptSectionDialog()
    {
        InitializeComponent();
        NameBox.AddHandler(TextInputEvent, NameBox_TextInput, RoutingStrategies.Tunnel);
    }

    private void NameBox_TextInput(object sender, TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text) && !AllowedCharactersRegex().IsMatch(e.Text))
        {
            e.Handled = true;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        NameBox.Focus();
    }

    [GeneratedRegex(@"[A-Za-z0-9_]")]
    private static partial Regex AllowedCharactersRegex();
}

