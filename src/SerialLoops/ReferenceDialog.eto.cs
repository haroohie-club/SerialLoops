using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops
{
    partial class ReferenceDialog : FindItemsDialog
    {
        public ReferenceMode Mode;
        public ItemDescription Item;
        
        public ReferenceDialog(ItemDescription item, ReferenceMode mode, Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log)
        {
            Item = item;
            Mode = mode;
            Project = project;
            Explorer = explorer;
            Tabs = tabs;
            Log = log;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = Mode == ReferenceMode.REFERENCES_TO ? $"References to {Item.Name}" : $"Items referenced by {Item.Name}";
            MinimumSize = new Size(400, 275);
            Padding = 10;

            List<ItemDescription> results = GetResults();
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Spacing = 10,
                Padding = 10,
                Items =
                {
                    $"{results.Count} items that {(Mode == ReferenceMode.REFERENCES_TO ? "reference" : "are referenced by")} {Item.Name}:",
                    new ItemResultsPanel(results, Log) { Dialog = this }
                }
            };
        }

        private List<ItemDescription> GetResults()
        {
            switch (Item.Type)
            {
                case ItemDescription.ItemType.Background:
                    if (Mode == ReferenceMode.REFERENCES_TO)
                    {
                        BackgroundItem bg = (BackgroundItem)Item;
                        return Project.Items.Where(i => bg.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                    }
                    return new List<ItemDescription>();
                case ItemDescription.ItemType.Character_Sprite:
                    if (Mode == ReferenceMode.REFERENCES_TO)
                    {
                        CharacterSpriteItem sprite = (CharacterSpriteItem)Item;
                        return Project.Items.Where(i => sprite.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                    }
                    return new List<ItemDescription>();
                case ItemDescription.ItemType.Chibi:
                    if (Mode == ReferenceMode.REFERENCES_TO)
                    {
                        ChibiItem chibi = (ChibiItem)Item;
                        return Project.Items.Where(i => chibi.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                    }
                    return new List<ItemDescription>();
                default:
                    return new List<ItemDescription>(); //todo
            }
        }

        public enum ReferenceMode
        {
            REFERENCES_TO,
            REFERENCED_BY
        }

    }

}
