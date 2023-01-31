using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SerialLoops
{
    internal class EditorTabsPanel : Panel
    {

        public static readonly Size EDITOR_BASE_SIZE = new(500, 420);

        public TabControl Tabs { get; private set; }
        
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

            Tabs = new();
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
            foreach (TabPage page in Tabs.Pages)
            {
                if (page.Text.Equals(item.Name))
                {
                    Tabs.SelectedPage = page;
                    return;
                }
            }

            // Open a new editor for the item -- This is where the item can be loaded from the project files
            switch (item.Type)
            {
                case ItemDescription.ItemType.Map:
                    Tabs.Pages.Add(new MapEditor(new MapItem(item.Name)));
                    break;
                case ItemDescription.ItemType.Dialogue:
                    Tabs.Pages.Add(new DialogueEditor(new DialogueItem(item.Name)));
                    break;
            }
        }
    }
}
