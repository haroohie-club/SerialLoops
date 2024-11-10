using Avalonia.Controls;

namespace SerialLoops.Views.Dialogs;

public partial class GraphicSelectionDialog : Window
{
    public GraphicSelectionDialog()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Filter.Focus();
    }
}