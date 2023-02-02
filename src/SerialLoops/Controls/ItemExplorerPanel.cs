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
    public class ItemExplorerPanel : ItemListPanel
    {

        private readonly Project _project;
        private readonly EditorTabsPanel _tabs;
        
        public ItemExplorerPanel(Project project, EditorTabsPanel tabs, ILogger log) : base(project.Items, new Size(200, 420), false, log)
        {
            _project = project;
            _tabs = tabs;
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