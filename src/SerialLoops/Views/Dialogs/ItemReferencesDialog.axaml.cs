using Avalonia.Controls;
using Avalonia.Input;
using SerialLoops.ViewModels.Dialogs;

namespace SerialLoops.Views.Dialogs;

public partial class ItemReferencesDialog : Window
{
    public ItemReferencesDialog()
    {
        InitializeComponent();
    }

    private void Viewer_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ((ItemReferencesDialogViewModel)DataContext)?.OpenItemCommand.Execute(Viewer);
        }
    }
}

