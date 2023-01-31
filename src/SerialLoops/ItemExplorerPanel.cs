using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;

namespace SerialLoops
{
    internal class ItemExplorerPanel : Scrollable
    {

        public static readonly Size ITEM_EXPLORER_BASE_SIZE = new(200, 420);

        private ListBox _items;
        private readonly Project _project;
        private readonly EditorTabsPanel _tabs;

        public ItemExplorerPanel(Project project, EditorTabsPanel tabs)
        {
            _project = project;
            _tabs = tabs;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            MinimumSize = ITEM_EXPLORER_BASE_SIZE;
            Padding = 0;

            _items = new ListBox
            {
                Size = ITEM_EXPLORER_BASE_SIZE
            };
            _items.Activated += ItemList_ItemSelected;

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    _items
                }
            };

            // Test items
            foreach (ItemDescription item in _project.GetItems())
            {
                _items.Items.Add(item.Name);
            }
        }

        private void ItemList_ItemSelected(object sender, EventArgs e)
        {
            if (_items.SelectedIndex != -1)
            {
                ItemDescription item = _project.GetItems()[_items.SelectedIndex];
                _tabs.OpenTab(item);
            }

        }
    }
}