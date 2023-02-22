using Eto.Drawing;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;

namespace SerialLoops.Controls
{
    public class ItemResultsPanel : ItemListPanel
    {
        public FindItemsDialog Dialog;
        public ItemResultsPanel(List<ItemDescription> results, ILogger log, bool expandItems = true) : base(results, new Size(280, 185), expandItems, log) { }

        protected override void ItemList_ItemClicked(object sender, EventArgs e)
        {
            if (sender is SectionListTreeGridView view)
            {
                ItemDescription item = Dialog.Project.FindItem(view.SelectedItem?.Text);
                if (item != null)
                {
                    Dialog.Tabs.OpenTab(item, _log);
                }
                Dialog.Close();
            }
        }
    }
}