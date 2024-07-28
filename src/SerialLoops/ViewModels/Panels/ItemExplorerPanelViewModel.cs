using Avalonia.Controls;
using Avalonia.Input;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Views.Panels;

namespace SerialLoops.ViewModels.Panels
{
    public class ItemExplorerPanelViewModel : ItemListPanel
    {
        private Project _project;
        private EditorTabsPanelViewModel _tabs;

        public TextBox SearchBox { get; private set; }

        public void Initialize(ItemExplorerPanel panel, Project project, EditorTabsPanelViewModel tabs, TextBox searchBox, ILogger log)
        {
            InitializeItems(project.Items, panel.Viewer, new(200, 420), false, log);
            _project = project;
            _tabs = tabs;
            SearchBox = panel.Search;
            Viewer.SelectionChanged += Viewer_SelectionChanged;
        }

        public override void ItemList_ItemDoubleClicked(object sender, TappedEventArgs args)
        {

        }

        private void Viewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ItemDescription item = _project.FindItem(((ITreeItem)Viewer.SelectedItem).Text);
            if (item is not null)
            {
                
            }
        }
    }
}
