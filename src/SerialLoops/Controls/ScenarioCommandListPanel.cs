using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Editors;
using System.Collections.Generic;
using static HaruhiChokuretsuLib.Archive.Event.ScenarioCommand;

namespace SerialLoops.Controls
{
    public class ScenarioCommandListPanel : Panel
    {
        public List<(ScenarioVerb, string)> Commands 
        {
            get => _commands; 
            set
            {
                _commands = value;
                Viewer?.Items.Clear();
                _commands.ForEach(c => Viewer?.Items.Add($"{c.Item1} {c.Item2}"));
            }
        }
        public ListBox Viewer { get; private set; }
        public (ScenarioVerb Verb, string Parameter)? SelectedCommand { get => Viewer.SelectedIndex != -1 ? _commands[Viewer.SelectedIndex] : null; }

        private List<(ScenarioVerb, string)> _commands;
        protected ILogger _log;
        private readonly Size _size;
        private Editor _editor;

        public ScenarioCommandListPanel(List<(ScenarioVerb, string)> commands, Size size, Editor editor, ILogger log)
        {
            Commands = commands;
            _log = log;
            _size = size;
            _editor = editor;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Viewer = new ListBox()
            {
                Size = _size,
            };
            MinimumSize = _size;
            Padding = 0;
            Content = new TableLayout(Viewer);
            Commands.ForEach(c => Viewer.Items.Add($"{c.Item1} {c.Item2}"));
        }

    }

}