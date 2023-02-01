using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using System;

namespace SerialLoops.Editors
{
    public abstract class Editor : DocumentPage
    {
        private ILogger _log;
        public ItemDescription Description { get; private set; }

        public Editor(ItemDescription description, ILogger log)
        {
            Description = description;
            _log = log;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Text = Description.Name;
            MinimumSize = EditorTabsPanel.EDITOR_BASE_SIZE;
            try
            {
                Image = EditorTabsPanel.GetItemIcon(Description.Type);
            }
            catch (Exception exc)
            {
                _log.LogWarning($"Failed to load icon.\n{exc.Message}\n\n{exc.StackTrace}");
            }
            Padding = 10;

            Panel panel = GetEditorPanel();
            panel.MinimumSize = EditorTabsPanel.EDITOR_BASE_SIZE;
            Content = panel;
        }

        public abstract Panel GetEditorPanel();
        
    }
}
