﻿using Eto.Drawing;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;

namespace SerialLoops.Controls
{
    public class ItemResultsPanel(List<ItemDescription> results, ILogger log, bool expandItems = true) : ItemListPanel(results, new Size(280, 185), expandItems, log)
    {
        public FindItemsDialog Window { get; set; }

        protected override void ItemList_ItemClicked(object sender, EventArgs e)
        {
            if (sender is SectionListTreeGridView view)
            {
                ItemDescription item = Window.Project.FindItem(view.SelectedItem?.Text);
                if (item != null)
                {
                    Window.Tabs.OpenTab(item, _log);
                }
            }
        }
    }
}