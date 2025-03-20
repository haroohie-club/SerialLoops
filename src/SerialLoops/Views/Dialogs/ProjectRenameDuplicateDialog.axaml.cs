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
    }

    private void NameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.KeySymbol) && !Regex.IsMatch(e.KeySymbol, @"[A-Za-z\d-_\.]") && e.Key != Key.Back)
        {
            e.Handled = true;
            return;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        NameBox.Focus();
    }
}

