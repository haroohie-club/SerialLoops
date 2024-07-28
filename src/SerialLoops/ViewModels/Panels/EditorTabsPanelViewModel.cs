using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Views.Panels;
using Tabalonia.Controls;

namespace SerialLoops.ViewModels.Panels
{
    public class EditorTabsPanelViewModel : ViewModelBase
    {
        private EditorTabsPanel _tabsPanel;
        public TabsControl Tabs => _tabsPanel.Tabs;

        private Project _project;
        private ILogger _log;
        public MainWindowViewModel MainWindow { get; private set; }

        public void Initialize(EditorTabsPanel tabsPanel, MainWindowViewModel mainWindow, Project project, ILogger log)
        {
            _tabsPanel = tabsPanel;
            MainWindow = mainWindow;
            _project = project;
            _log = log;
        }
    }
}
