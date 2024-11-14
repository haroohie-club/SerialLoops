using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SerialLoops.Views.Dialogs;

public partial class AddScriptCommandDialog : Window
{
    public AddScriptCommandDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        CommandBox.Focus(NavigationMethod.Directional);
    }
}

