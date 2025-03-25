using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SerialLoops.Views.Dialogs;

public partial class ProjectCreationDialog : Window
{
    public ProjectCreationDialog()
    {
        InitializeComponent();
        NameBox.AddHandler(TextInputEvent, NameBox_TextInput, RoutingStrategies.Tunnel);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        NameBox.Focus();
    }

    private void NameBox_TextInput(object sender, TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text) && !AllowedCharactersRegex().IsMatch(e.Text))
        {
            e.Handled = true;
        }
    }

    [GeneratedRegex(@"[A-Za-z\d-_\.]")]
    private static partial Regex AllowedCharactersRegex();
}
