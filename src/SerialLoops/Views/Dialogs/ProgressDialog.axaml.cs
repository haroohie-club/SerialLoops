using Avalonia.Controls;

namespace SerialLoops.Views.Dialogs;

public partial class ProgressDialog : Window
{
    public ProgressDialog()
    {
        InitializeComponent();
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (!e.IsProgrammatic)
        {
            e.Cancel = true;
        }
    }
}

