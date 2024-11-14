using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Dialogs;

namespace SerialLoops.Views.Dialogs;

public partial class AddScriptCommandDialog : Window
{
    public AddScriptCommandDialog()
    {
        InitializeComponent();
        CommandBox.AddHandler(KeyDownEvent, CommandBox_OnKeyDown, RoutingStrategies.Tunnel);
    }
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        CommandBox.Focus();
        // We start with the box's dropdown open so that it forces all the command verbs
        // to load. We then close it here to avoid taking up a ton of screen real estate
        // unnecessarily.
        CommandBox.IsDropDownOpen = false;
    }

    private void CommandBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            ((AddScriptCommandDialogViewModel)DataContext!).CreateCommand.Execute(this);
        }
    }
}

