using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public abstract class Editor : DocumentPage
    {
        protected ILogger _log;
        protected IProgressTracker _tracker;
        protected Project _project;
        protected EditorTabsPanel _tabs;
        public ItemDescription Description { get; private set; }

        public Editor(ItemDescription description, ILogger log, IProgressTracker tracker, Project project = null, EditorTabsPanel tabs = null)
        {
            Description = description;
            _project = project;
            _tabs = tabs;
            _log = log;
            _tracker = tracker;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Text = Description.DisplayName;
            Image = ControlGenerator.GetItemIcon(Description.Type, _log);
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
