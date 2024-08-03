using System.Collections.ObjectModel;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.ViewModels.Editors;

namespace SerialLoops.ViewModels.Panels
{
    public class EditorTabsPanelViewModel : ViewModelBase
    {
        private Project _project;
        private ILogger _log;
        private EditorViewModel _selectedTab;

        public MainWindowViewModel MainWindow { get; private set; }
        [Reactive]
        public EditorViewModel SelectedTab { get; set; }

        public ICommand TabSwitchedCommand { get; set; }

        public ObservableCollection<EditorViewModel> Tabs { get; set; } = [];

        public EditorTabsPanelViewModel()
        {
            TabSwitchedCommand = ReactiveCommand.Create(OnTabSwitched);
        }

        public void Initialize(MainWindowViewModel mainWindow, Project project, ILogger log)
        {
            MainWindow = mainWindow;
            _project = project;
            _log = log;
        }

        public void OpenTab(ItemDescription item)
        {
            foreach (EditorViewModel tab in Tabs)
            {
                if (tab.Description.DisplayName.Equals(item.DisplayName))
                {
                    SelectedTab = tab;
                    return;
                }
            }

            EditorViewModel newTab = CreateTab(item);
            if (newTab is not null)
            {
                Tabs.Add(newTab);
                SelectedTab = newTab;
            }
        }

        private EditorViewModel CreateTab(ItemDescription item)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    return new BackgroundEditorViewModel((BackgroundItem)item, MainWindow, _project, _log);
                case ItemDescription.ItemType.BGM:
                    return new BackgroundMusicEditorViewModel((BackgroundMusicItem)item, MainWindow, _project, _log);
                case ItemDescription.ItemType.System_Texture:
                    return new SystemTextureEditorViewModel((SystemTextureItem)item, MainWindow, _project, _log);
                default:
                    _log.LogError(Strings.Invalid_item_type_);
                    return null;
            }
        }

        private void OnTabSwitched()
        {

        }

        public void OnTabClosed(EditorViewModel closedEditor)
        {
            if (closedEditor.Description.Type == ItemDescription.ItemType.BGM)
            {
                ((BackgroundMusicEditorViewModel)closedEditor).BgmPlayer.Stop();
            }
        }

        public void OnTabMiddleClicked()
        {
            Tabs.Remove(SelectedTab);
        }
    }
}
