using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;

namespace SerialLoops.Controls
{
    public class ItemExplorerPanel : ItemListPanel
    {
        private readonly Project _project;
        private readonly EditorTabsPanel _tabs;
        private readonly SearchBox _searchBox;
        
        public ItemExplorerPanel(Project project, EditorTabsPanel tabs, SearchBox searchBox, ILogger log) : base(project.Items, new Size(200, 420), false, log)
        {
            _project = project;
            _tabs = tabs;
            _searchBox = searchBox;
            _searchBox.TextChanging += SearchBox_TextChanging;
            Viewer.SelectedItemChanged += Viewer_SelectedItemChanged;
            if (Viewer.SelectedItem is not null)
            {
                ((TreeGridView)Viewer.Control).ContextMenu = Viewer.SelectedItem.Text.GetContextMenu(_project, this, _tabs, _log);
            }
        }

        private void SearchBox_TextChanging(object sender, TextChangingEventArgs e)
        {
            var searchTerm = e.NewText;
            Items = !string.IsNullOrWhiteSpace(searchTerm) ? _project.GetSearchResults(searchTerm) : _project.Items;
            ExpandItems = !string.IsNullOrEmpty(searchTerm);
        }

        private void Viewer_SelectedItemChanged(object sender, EventArgs e)
        {
            if (Viewer.SelectedItem is not null)
            {
                ((TreeGridView)Viewer.Control).ContextMenu = Viewer.SelectedItem.Text.GetContextMenu(_project, this, _tabs, _log);
                
            }
        }

        protected override void ItemList_ItemClicked(object sender, EventArgs e)
        {
            if (sender is SectionListTreeGridView view)
            {
                ItemDescription item = _project.FindItem(view.SelectedItem?.Text);
                if (item != null)
                {
                    _tabs.OpenTab(item, _log);
                }
            }
        }
    }
}