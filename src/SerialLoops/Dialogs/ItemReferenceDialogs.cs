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
            Title = string.Format(Application.Instance.Localize(this, "References to {0}"), Item.DisplayName);
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
                    string.Format(Application.Instance.Localize(this, "{0} items that reference {1}:"), results.Count, Item.Name),
                    new ItemResultsPanel(results, Log) { Window = this }
                }
            };
        }

    }

    public class OrphanedItemsDialog : FindItemsDialog
    {
        private static readonly ItemDescription.ItemType[] IGNORED_ORPHAN_TYPES = {
            ItemDescription.ItemType.Scenario
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
            Title = Application.Instance.Localize(this, "Orphaned Items");
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
                    string.Format(Application.Instance.Localize(this, "{0} items found that are not referenced by other items:"), results.Count),
                    new ItemResultsPanel(results, Log, false) { Window = this }
                }
            };
        }
    }

}
