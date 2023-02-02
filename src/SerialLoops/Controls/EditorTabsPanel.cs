using Eto.Drawing;
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

        public static Icon GetItemIcon(ItemDescription.ItemType type, ILogger log)
        {
            try
            {
                return Icon.FromResource($"SerialLoops.Icons.{type}.png").WithSize(16, 16);
            }
            catch (Exception exc)
            {
                log.LogWarning($"Failed to load icon.\n{exc.Message}\n\n{exc.StackTrace}");
                return null;
            }
        }

        internal void OpenTab(ItemDescription item, ILogger log)
        {
            // If a tab page with the name and type exists, switch to it
            foreach (DocumentPage page in Tabs.Pages)
            {
                if (page.Text == item.Name)
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

        internal static DocumentPage CreateTab(ItemDescription item, Project project, ILogger log)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    return new BackgroundEditor((BackgroundItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Chibi:
                    return new ChibiEditor((ChibiItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Map:
                    return new MapEditor((MapItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Puzzle:
                    return new PuzzleEditor((PuzzleItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Script:
                    return new ScriptEditor((ScriptItem)project.Items.First(i => i.Name == item.Name), log);
                default:
                    throw new ArgumentException("Invalid item type");
            }
        }
    }
}
