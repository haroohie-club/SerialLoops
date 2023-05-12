using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class ReferencesDialog : FindItemsDialog
    {
        public ItemDescription Item;

        public ReferencesDialog(ItemDescription item, Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log)
        {
            Item = item;
            Project = project;
            Explorer = explorer;
            Tabs = tabs;
            Log = log;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = $"References to {Item.DisplayName}";
            MinimumSize = new Size(400, 275);
            Padding = 10;

            List<ItemDescription> results = Item.GetReferencesTo(Project);
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Spacing = 10,
                Padding = 10,
                Items =
                {
                    $"{results.Count} items that reference {Item.Name}:",
                    new ItemResultsPanel(results, Log) { Dialog = this }
                }
            };
        }

    }

    public class OrphanedItemsDialog : FindItemsDialog
    {
        private static readonly ItemDescription.ItemType[] IGNORED_ORPHAN_TYPES = {
            ItemDescription.ItemType.Scenario,
            ItemDescription.ItemType.Character
        };

        public OrphanedItemsDialog(Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log)
        {
            Project = project;
            Explorer = explorer;
            Tabs = tabs;
            Log = log;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Orphaned Items";
            MinimumSize = new Size(400, 275);
            Padding = 10;

            List<ItemDescription> results = Project.Items.FindAll(i => !IGNORED_ORPHAN_TYPES.Contains(i.Type) && i.GetReferencesTo(Project).Count == 0);
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Spacing = 10,
                Padding = 10,
                Items =
                {
                    $"{results.Count} items found that are not referenced by other items:",
                    new ItemResultsPanel(results, Log, false) { Dialog = this }
                }
            };
        }
    }

}
