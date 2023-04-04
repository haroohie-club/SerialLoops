using System.Collections.Generic;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public abstract class Editor : DocumentPage
    {
        public const string EDITOR_TOOLBAR_TAG = "editor";

        protected ILogger _log;
        protected Project _project;
        protected EditorTabsPanel _tabs;

        public List<Command> EditorCommands { get; set; } = new();
        public ItemDescription Description { get; }

        protected Editor(ItemDescription description, ILogger log, Project project = null, EditorTabsPanel tabs = null)
        {
            Description = description;
            _project = project;
            _tabs = tabs;
            _log = log;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Text = Description.DisplayNameWithStatus;
            Image = ControlGenerator.GetItemIcon(Description.Type, _log);
            Padding = 10;
            Content = GetEditorPanel();
        }

        public abstract Container GetEditorPanel();

        public void UpdateTabTitle(bool saved, Control caller = null)
        {
            if (Description.UnsavedChanges == !saved)
            {
                // no need to update then, let's save on perf
                return;
            }
            int caretIndex = 0;
            if (caller is not null && caller is TextBox textBox)
            {
                // This hack is necessary because macOS handles .Focus by selecting all text in the textbox
                caretIndex = textBox.CaretIndex;
            }
            Description.UnsavedChanges = !saved;
            Text = Description.DisplayNameWithStatus;
            if (caller is not null && caller is TextBox box)
            {
                box.Focus();
                box.CaretIndex = caretIndex;
            }
            else
            {
                caller?.Focus();
            }
        }
    }
}
