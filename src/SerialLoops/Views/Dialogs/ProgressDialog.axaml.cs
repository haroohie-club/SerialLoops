using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Dialogs;

namespace SerialLoops.Views.Dialogs;

public partial class ProgressDialog : Window
{
    public ProgressDialog()
    {
        InitializeComponent();
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (!e.IsProgrammatic)
        {
            e.Cancel = true;
        }
    }

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ((ProgressDialogViewModel)DataContext!).OnLoaded();
        Close();
    }
}

