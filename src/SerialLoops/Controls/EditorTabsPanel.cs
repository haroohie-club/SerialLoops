using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Linq;

namespace SerialLoops.Controls
{
    public class EditorTabsPanel : Panel
    {
        public DocumentControl Tabs { get; private set; }

        private readonly Project _project;

        public EditorTabsPanel(Project project)
        {
            _project = project;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Padding = 0;

            Tabs = new()
            {
                AllowReordering = true,
                Enabled = true
            };
            Content = new TableLayout(Tabs);
        }

        internal void OpenTab(ItemDescription item, ILogger log)
        {
            // If a tab page with the name and type exists, switch to it
            foreach (DocumentPage page in Tabs.Pages)
            {
                if (page.Text == item.Name || page.Text == $"{item.Name} *")
                {
                    Tabs.SelectedPage = page;
                    return;
                }
            }

            // Open a new editor for the item -- This is where the item can be loaded from the project files
            DocumentPage newPage = CreateTab(item, _project, log);
            newPage.ContextMenu = new TabContextMenu(this, newPage, log);

            Tabs.Pages.Add(newPage);
            Tabs.SelectedPage = newPage;
        }

        internal DocumentPage CreateTab(ItemDescription item, Project project, ILogger log)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    return new BackgroundEditor((BackgroundItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Character_Sprite:
                    return new CharacterSpriteEditor((CharacterSpriteItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Chibi:
                    return new ChibiEditor((ChibiItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Map:
                    return new MapEditor((MapItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Puzzle:
                    return new PuzzleEditor((PuzzleItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Scenario:
                    return new ScenarioEditor((ScenarioItem)project.Items.First(i => i.Name == item.Name), log, project, this);
                case ItemDescription.ItemType.Script:
                    return new ScriptEditor((ScriptItem)project.Items.First(i => i.Name == item.Name), project, log, this);
                default:
                    throw new ArgumentException("Invalid item type");
            }
        }
    }
}
