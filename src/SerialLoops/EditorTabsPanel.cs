using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Linq;
using System.Reflection;

namespace SerialLoops
{
    public class EditorTabsPanel : Panel
    {

        public static readonly Size EDITOR_BASE_SIZE = new(500, 420);

        public DocumentControl Tabs { get; private set; }
        
        private readonly Project _project;

        public EditorTabsPanel(Project project)
        {
            _project = project;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            MinimumSize = EDITOR_BASE_SIZE;
            Padding = 0;

            Tabs = new()
            {
                AllowReordering = true,
                Enabled = true
            };
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                MinimumSize = EDITOR_BASE_SIZE,
                Items =
                {
                    Tabs
                }
            };
        }

        public static Icon GetItemIcon(ItemDescription.ItemType type)
        {
            return Icon.FromResource($"SerialLoops.Icons.{type}.png", Assembly.GetExecutingAssembly()).WithSize(16, 16);
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
            Tabs.Pages.Add(newPage);
            Tabs.SelectedPage = newPage;

        }

        internal static DocumentPage CreateTab(ItemDescription item, Project project, ILogger log)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    return new BackgroundEditor((BackgroundItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Map:
                    return new MapEditor((MapItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Script:
                    return new ScriptEditor((ScriptItem)project.Items.First(i => i.Name == item.Name), log);
                default:
                    throw new ArgumentException("Invalid item type");
            }
        }
    }
}
