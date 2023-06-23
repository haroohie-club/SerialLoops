using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using static HaruhiChokuretsuLib.Archive.Event.ScenarioCommand;

namespace SerialLoops.Controls
{
    public class ScenarioCommandListPanel : Panel
    {
        private bool _alreadySetting = false;
        public List<(ScenarioVerb Verb, string Parameter)> Commands
        {
            get => _commands;
            set
            {
                if (_alreadySetting)
                {
                    return;
                }

                _alreadySetting = true;
                _commands = value;
                if (Viewer is not null)
                {
                    int selectedIndex = Viewer.SelectedIndex;
                    Viewer.Items.Clear();
                    Viewer.Items.AddRange(_commands.Select(c => new ListItem { Text = $"{c.Verb} {c.Parameter}" }));
                    if (selectedIndex < 0)
                    {
                        selectedIndex = 0;
                    }
                    if (_commands.Count > selectedIndex)
                    {
                        Viewer.SelectedIndex = selectedIndex;
                        Viewer.Focus();
                    }
                    else
                    {
                        Viewer.SelectedIndex = -1;
                    }
                }
                _alreadySetting = false;
            }
        }
        public ListBox Viewer { get; private set; }
        public (ScenarioVerb Verb, string Parameter)? SelectedCommand { get => Viewer.SelectedIndex != -1 ? _commands[Viewer.SelectedIndex] : null; }

        private List<(ScenarioVerb Verb, string Parameter)> _commands;
        protected ILogger _log;
        private readonly Size _size;

        public ScenarioCommandListPanel(List<(ScenarioVerb Verb, string Parameter)> commands, Size size, ILogger log)
        {
            Commands = commands;
            _log = log;
            _size = size;
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
            Commands.ForEach(c => Viewer.Items.Add($"{c.Verb} {c.Parameter}"));
        }

    }

}