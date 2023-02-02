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
    public class ItemResultsPanel : ItemListPanel
    {

        public Project Project;
        public EditorTabsPanel Tabs;
        public Dialog Dialog;

        public ItemResultsPanel(List<ItemDescription> results, ILogger log) : base(results, new Size(280, 185), true, log) { }

        protected override void ItemList_ItemClicked(object sender, EventArgs e)
        {
            if (sender is SectionListTreeGridView view)
            {
                ItemDescription item = Project.FindItem(view.SelectedItem?.Text);
                if (item != null)
                {
                    Tabs.OpenTab(item, _log);
                }
                Dialog.Close();
            }
        }
    }
}