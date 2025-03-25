using Avalonia.Controls;
using SerialLoops.ViewModels.Editors;
using SerialLoops.ViewModels.Panels;
using Tabalonia.Controls;

namespace SerialLoops.Views.Panels;

public partial class EditorTabsPanel : Panel
{

    public EditorTabsPanel()
    {
        InitializeComponent();
    }

    private async void Tabs_ContainerClearing(object sender, ContainerClearingEventArgs e)
    {
        await ((EditorTabsPanelViewModel)DataContext!).OnTabClosed((EditorViewModel)e.Container.DataContext,
            ((TabsControl)sender).IndexFromContainer(e.Container));
    }
}
