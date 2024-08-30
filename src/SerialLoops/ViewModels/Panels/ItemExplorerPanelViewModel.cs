using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;

namespace SerialLoops.ViewModels.Panels
{
    public class ItemExplorerPanelViewModel : ItemListPanel
    {
        private Project _project;
        private EditorTabsPanelViewModel _tabs;

        public ICommand SearchCommand { get; set; }

        public ItemExplorerPanelViewModel(Project project, EditorTabsPanelViewModel tabs, ILogger log)
        {
            InitializeItems(project.Items, false, log);
            _project = project;
            _tabs = tabs;
            SearchCommand = ReactiveCommand.Create<string>(Search);
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

        private void Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                ExpandItems = false;
                Items = _project.Items;
            }
            else
            {
                ExpandItems = true;
                Items = [.. _project.Items.Where(i => i.DisplayName.Contains(query, System.StringComparison.OrdinalIgnoreCase))];
            }
        }
    }
}
