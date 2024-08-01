using Avalonia.Controls;
using SerialLoops.ViewModels.Editors;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.Views.Panels
{
    public partial class EditorTabsPanel : Panel
    {

        public EditorTabsPanel()
        {
            InitializeComponent();
        }

        private void Tabs_ContainerClearing(object? sender, ContainerClearingEventArgs e)
        {
            ((EditorTabsPanelViewModel)DataContext).OnTabClosed((EditorViewModel)e.Container.DataContext);
        }
    }
}
