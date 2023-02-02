using Eto.Drawing;
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
        private List<ItemDescription> _items;

        public List<ItemDescription> Items { 
            protected get { return _items;  }
            set
            {
                _items = value;
                if (_viewer is not null)
                {
                    _viewer.SetContents(GetSections());
                }
            }
        }

        protected ILogger _log;
        protected SectionListTreeGridView _viewer;

        private readonly Size _size;

        protected ItemListPanel(List<ItemDescription> items, Size size, ILogger log)
        {
            Items = items;
            _log = log;
            _size = size;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            _viewer = new SectionListTreeGridView(GetSections(), _size);
            MinimumSize = _size;
            Padding = 0;
            Content = new TableLayout(_viewer.Control);
            _viewer.Activated += ItemList_ItemClicked;
        }

        private IEnumerable<Section> GetSections()
        {
             return Items.GroupBy(i => i.Type).OrderBy(g => g.Key)
                .Select(g => new Section($"{g.Key}s", g.Select(i => new Section() { Text = i.Name }), EditorTabsPanel.GetItemIcon(g.Key, _log)));
        }

        protected abstract void ItemList_ItemClicked(object sender, EventArgs e);

    }
}
