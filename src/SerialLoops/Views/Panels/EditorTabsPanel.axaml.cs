using Avalonia.Controls;

namespace SerialLoops.Views.Panels
{
    public partial class EditorTabsPanel : Panel
    {

        public EditorTabsPanel()
        {
            InitializeComponent();
        }

        private void Tabs_PageChanged(object? sender, SelectionChangedEventArgs e)
        {
            
        }

        private void Tabs_PageClosed(object? sender, ContainerClearingEventArgs e)
        {
            
        }
    }
}