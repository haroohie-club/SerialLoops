using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Editors
{
    public class ScenarioEditor : Editor
    {
        private ScenarioItem _scenario;

        public ScenarioEditor(ScenarioItem item, ILogger log, Project project, EditorTabsPanel tabs) : base(item, log, project, tabs)
        {
        }

        public override Scrollable GetEditorPanel()
        {
            _scenario = (ScenarioItem)Description;
            StackLayout mainLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
            };

            IEnumerable<ListItem> verbs = new string[]
            {
                "NEW_GAME",
                "SAVE",
                "LOAD_SCENE",
                "PUZZLE_PHASE",
                "ROUTE_SELECT",
                "STOP",
                "SAVE2",
                "TOPICS",
                "COMPANION_SELECT",
                "PLAY_VIDEO",
                "NOP",
                "UNKNOWN0B",
                "UNLOCK",
                "END",
            }.Select(v => new ListItem() { Text = v, Key = v });

            foreach ((string verb, string parameter) in _scenario.ScenarioCommands)
            {
                StackLayout commandLayout = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                };

                DropDown commandDropDown = new();
                commandDropDown.Items.AddRange(verbs);
                commandDropDown.SelectedKey = verb;
                commandLayout.Items.Add(commandDropDown);
                switch (verb)
                {
                    case "LOAD_SCENE":
                    case "PUZZLE_PHASE":
                        LinkButton link = new() { Text = parameter };
                        link.Click += LinkClick_Click;
                        commandLayout.Items.Add(link);
                        break;

                    default:
                        commandLayout.Items.Add(new TextBox { Text = parameter });
                        break;
                }

                mainLayout.Items.Add(commandLayout);
            }

            return new Scrollable() { Content = mainLayout };
        }

        private void LinkClick_Click(object sender, System.EventArgs e)
        {
            LinkButton link = (LinkButton)sender;
            ItemDescription item = _project.FindItem(link.Text);
            if (item != null)
            {
                _tabs.OpenTab(item, _log);
            }
        }
    }
}
