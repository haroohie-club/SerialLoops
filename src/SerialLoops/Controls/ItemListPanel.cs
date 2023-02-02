﻿using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Controls
{
    public abstract class ItemListPanel : Scrollable
    {
        public List<ItemDescription> Items
        {
            protected get { return _items; }
            set
            {
                _items = value;
                Viewer?.SetContents(GetSections(), _expandItems);
            }
        }
        public SectionListTreeGridView Viewer { get; private set; }
        
        protected ILogger _log;
        private readonly Size _size;
        private List<ItemDescription> _items;
        private readonly bool _expandItems;

        protected ItemListPanel(List<ItemDescription> items, Size size, bool expandItems, ILogger log)
        {
            Items = items;
            _log = log;
            _size = size;
            _expandItems = expandItems;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Viewer = new SectionListTreeGridView(GetSections(), _size, _expandItems);
            MinimumSize = _size;
            Padding = 0;
            Content = new TableLayout(Viewer.Control);
            Viewer.Activated += ItemList_ItemClicked;
        }

        private IEnumerable<Section> GetSections()
        {
             return Items.GroupBy(i => i.Type).OrderBy(g => g.Key)
                .Select(g => new Section($"{g.Key}s", g.Select(i => new Section() { Text = i.Name }), EditorTabsPanel.GetItemIcon(g.Key, _log)));
        }

        protected abstract void ItemList_ItemClicked(object sender, EventArgs e);

    }
}
