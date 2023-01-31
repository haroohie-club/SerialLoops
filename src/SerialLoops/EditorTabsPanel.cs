using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;

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
            //todo battle with this and figure out loading from a resx?
            //return Icon.FromResource($"SerialLoops.Icons.{type}.png", Assembly.GetExecutingAssembly());
            throw new NotImplementedException();
        }

        internal void OpenTab(ItemDescription item)
        {
            // If a tab page with the name and type exists, switch to it
            foreach (DocumentPage page in Tabs.Pages)
            {
                if (page.Text.Equals(item.Name))
                {
                    Tabs.SelectedPage = page;
                    return;
                }
            }

            // Open a new editor for the item -- This is where the item can be loaded from the project files
            DocumentPage newPage = CreateTab(item);
            Tabs.Pages.Add(newPage);
            Tabs.SelectedPage = newPage;

        }

        internal static DocumentPage CreateTab(ItemDescription item)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Map:
                    return (new MapEditor(new MapItem(item.Name)));
                case ItemDescription.ItemType.Dialogue:
                    return new DialogueEditor(new DialogueItem(item.Name));
                default:
                    throw new ArgumentException("Invalid item type");
            }
        }
    }
}
