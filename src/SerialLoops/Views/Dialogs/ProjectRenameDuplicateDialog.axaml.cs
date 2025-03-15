using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SerialLoops.Views.Dialogs;

public partial class ProjectRenameDuplicateDialog : Window
{
    public ProjectRenameDuplicateDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        NameBox.Focus();
    }
}

