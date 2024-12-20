using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.Views.Panels;

public partial class ItemExplorerPanel : Panel
{
    public static readonly AvaloniaProperty<bool?> ExpandItemsProperty = AvaloniaProperty.Register<ItemExplorerPanel, bool?>(nameof(ExpandItems));

    public bool? ExpandItems
    {
        get => this.GetValue<bool?>(ExpandItemsProperty);
        set => SetValue(ExpandItemsProperty, value);
    }

    public ItemExplorerPanel()
    {
        InitializeComponent();
    }

    private void Viewer_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ((ItemExplorerPanelViewModel)DataContext)?.OpenItemCommand.Execute(Viewer);
        }
    }
}
