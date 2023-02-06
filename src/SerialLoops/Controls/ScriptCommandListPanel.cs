using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Script;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Controls
{
    public class ScriptCommandListPanel : Panel
    {
        public Dictionary<ScriptSection, List<ScriptItemCommand>> Commands
        {
            protected get { return _commands; }
            set
            {
                _commands = value;
                Viewer?.SetContents(GetSections(), _expandItems);
            }
        }
        public ScriptCommandSectionTreeGridView Viewer { get; private set; }

        protected ILogger _log;
        private readonly Size _size;
        private Dictionary<ScriptSection, List<ScriptItemCommand>> _commands;
        private readonly bool _expandItems;

        public ScriptCommandListPanel(Dictionary<ScriptSection, List<ScriptItemCommand>> commands, Size size, bool expandItems, ILogger log)
        {
            Commands = commands;
            _log = log;
            _size = size;
            _expandItems = expandItems;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Viewer = new ScriptCommandSectionTreeGridView(GetSections(), _size, _expandItems);
            MinimumSize = _size;
            Padding = 0;
            Content = new TableLayout(Viewer.Control);
        }

        private IEnumerable<ScriptCommandSectionEntry> GetSections()
        {
            foreach (ScriptSection section in Commands.Keys)
            {
                List<ScriptCommandSectionEntry> commands = new();
                foreach (ScriptItemCommand command in Commands[section])
                {
                    commands.Add(new(command));
                }
                ScriptCommandSectionEntry s = new(section.Name, commands);
            }
            return Commands.Select(s => new ScriptCommandSectionEntry(s.Key.Name, s.Value.Select(c => new ScriptCommandSectionEntry(c))));
        }
    }
}
