using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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

            IEnumerable<Section> sections = _project.Items.GroupBy(i => i.Type).OrderBy(g => g.Key)
                .Select(g => new Section($"{g.Key}s", g.Select(i => new Section() { Text = i.Name }), EditorTabsPanel.GetItemIcon(g.Key, _log)));

            _items = new SectionListTreeGridView(sections, ITEM_EXPLORER_BASE_SIZE);
            _items.Activated += ItemList_ItemClicked;

            Content = new TableLayout(_items.Control);
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