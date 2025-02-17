using Avalonia.Controls;
using SerialLoops.ViewModels.Editors;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.Views.Panels;

public partial class EditorTabsPanel : Panel
{

    public EditorTabsPanel()
    {
        InitializeComponent();
    }

    private async void Tabs_ContainerClearing(object sender, ContainerClearingEventArgs e)
    {
        await ((EditorTabsPanelViewModel)DataContext!).OnTabClosed((EditorViewModel)e.Container.DataContext);
    }
}
