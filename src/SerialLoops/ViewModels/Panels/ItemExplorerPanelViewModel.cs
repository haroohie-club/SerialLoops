using Avalonia.Controls;
using Avalonia.Input;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;

namespace SerialLoops.ViewModels.Panels
{
    public class ItemExplorerPanelViewModel : ItemListPanel
    {
        private Project _project;
        private EditorTabsPanelViewModel _tabs;

        public TextBox SearchBox { get; private set; }

        public void Initialize(Project project, EditorTabsPanelViewModel tabs, TextBox searchBox, ILogger log)
        {
            InitializeItems(project.Items, false, log);
            _project = project;
            _tabs = tabs;
        }

        public void SetupExplorer(TreeView viewer)
        {
            SetupViewer(viewer);
            viewer.SelectionChanged += Viewer_SelectionChanged;
        }

        public override void ItemList_ItemDoubleClicked(object sender, TappedEventArgs args)
        {
            ItemDescription item = _project.FindItem(((ITreeItem)((TreeView)sender).SelectedItem)?.Text);
            if (item is not null)
            {
                _tabs.OpenTab(item);
            }
        }

        private void Viewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
