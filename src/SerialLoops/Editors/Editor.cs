using Eto.Forms;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public abstract class Editor : DocumentPage
    {

        public ItemDescription Description { get; private set; }

        public Editor(ItemDescription description)
        {
            Description = description;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Text = Description.Name;
            MinimumSize = EditorTabsPanel.EDITOR_BASE_SIZE;
            //Image = EditorTabsPanel.GetItemIcon(Description.Type);
            Padding = 10;

            Panel panel = GetEditorPanel();
            panel.MinimumSize = EditorTabsPanel.EDITOR_BASE_SIZE;
            Content = panel;
        }

        public abstract Panel GetEditorPanel();
        
    }
}
