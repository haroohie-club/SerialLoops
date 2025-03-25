using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SerialLoops.Views.Dialogs;

public partial class ProjectRenameDuplicateDialog : Window
{
    public ProjectRenameDuplicateDialog()
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

    [GeneratedRegex(@"[A-Za-z\d-_\.]")]
    private static partial Regex AllowedCharactersRegex();
}

