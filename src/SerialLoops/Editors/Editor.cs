using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;

namespace SerialLoops.Editors
{
    public abstract class Editor : DocumentPage
    {
        protected ILogger _log;
        protected Project _project;
        protected EditorTabsPanel _tabs;
        public ItemDescription Description { get; private set; }

        public Editor(ItemDescription description, ILogger log, Project project = null, EditorTabsPanel tabs = null)
        {
            Description = description;
            _project = project;
            _tabs = tabs;
            _log = log;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Text = Description.DisplayName;
            Image = EditorTabsPanel.GetItemIcon(Description.Type, _log);
            Padding = 10;
            Content = GetEditorPanel();
        }

        public abstract Container GetEditorPanel();
        
        public void UpdateTabTitle(bool saved)
        {
            Description.UnsavedChanges = !saved;
            Text = Description.DisplayName;
        }
    }
}
