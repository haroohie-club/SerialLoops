using System.Collections.ObjectModel;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.ViewModels.Editors;
using SerialLoops.Views.Panels;
using Tabalonia.Controls;

namespace SerialLoops.ViewModels.Panels
{
    public class EditorTabsPanelViewModel : ViewModelBase
    {
        private TabsControl _tabs;
        private Project _project;
        private ILogger _log;
        public MainWindowViewModel MainWindow { get; private set; }

        public ObservableCollection<EditorViewModel> Tabs { get; set; } = [];

        public void Initialize(MainWindowViewModel mainWindow, Project project, ILogger log)
        {
            MainWindow = mainWindow;
            _project = project;
            _log = log;
        }

        public void InitializeTabs(EditorTabsPanel tabs)
        {
            _tabs = tabs.Tabs;
        }


        public void OpenTab(ItemDescription item)
        {
            foreach (EditorViewModel tab in Tabs)
            {
                if (tab.Description.DisplayName.Equals(item.DisplayName))
                {
                    _tabs.SelectedItem = tab;
                    return;
                }
            }

            EditorViewModel newTab = CreateTab(item);
            Tabs.Add(newTab);
            _tabs.SelectedItem = newTab;
        }

        private EditorViewModel CreateTab(ItemDescription item)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    return new BackgroundEditorViewModel((BackgroundItem)item, MainWindow, _project, _log);
                case ItemDescription.ItemType.BGM:
                    return new BackgroundMusicEditorViewModel((BackgroundMusicItem)item, MainWindow, _project, _log);
                default:
                    _log.LogError(Strings.Invalid_item_type_);
                    return null;
            }
        }
    }
}
