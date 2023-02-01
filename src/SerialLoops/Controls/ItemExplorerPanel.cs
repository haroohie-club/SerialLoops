using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;

namespace SerialLoops.Controls
{
    public class ItemExplorerPanel : Scrollable
    {
        private ILogger _log;
        public static readonly Size ITEM_EXPLORER_BASE_SIZE = new(200, 420);

        private SectionListTreeGridView _items;
        private readonly Project _project;
        private readonly EditorTabsPanel _tabs;

        public ItemExplorerPanel(Project project, EditorTabsPanel tabs, ILogger log)
        {
            _log = log;
            _project = project;
            _tabs = tabs;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            MinimumSize = ITEM_EXPLORER_BASE_SIZE;
            Padding = 0;

            // Test items
            Dictionary<ItemDescription.ItemType, Section> itemTypes = new();
            foreach (ItemDescription item in _project.Items)
            {
                if (!itemTypes.ContainsKey(item.Type))
                {
                    itemTypes[item.Type] = new Section
                    {
                        Text = item.Type.ToString()
                    };

                }
                itemTypes[item.Type].Add(new Section
                {
                    Text = item.Name
                });
            }
            foreach (var category in itemTypes.Values)
            {
                category.Sort((x, y) => string.Compare(x.Text, y.Text, StringComparison.CurrentCultureIgnoreCase));
            }

            _items = new SectionListTreeGridView(itemTypes.Values, ITEM_EXPLORER_BASE_SIZE);
            _items.Activated += ItemList_ItemClicked;

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    _items.Control
                }
            };

        }

        private void ItemList_ItemClicked(object sender, EventArgs e)
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