using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SerialLoops.Editors
{
    public class ScenarioEditor : Editor
    {
        private ScenarioItem _scenario;

        public ScenarioEditor(ScenarioItem item, ILogger log, Project project, EditorTabsPanel tabs) : base(item, log, project, tabs)
        {
        }

        public override Container GetEditorPanel()
        {
            _scenario = (ScenarioItem)Description;

            TableLayout tableLayout = new()
            {
                Spacing = new Size(5, 5)
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
                TableRow row = new();

                DropDown commandDropDown = new();
                commandDropDown.Items.AddRange(verbs);
                commandDropDown.SelectedKey = verb;
                row.Cells.Add(new TableCell(commandDropDown));
                switch (verb)
                {
                    case "LOAD_SCENE":
                    case "PUZZLE_PHASE":
                        LinkButton link = new() { Text = parameter };
                        link.Click += LinkClick_Click;
                        row.Cells.Add(new TableCell(link));
                        break;

                    default:
                        row.Cells.Add(new TableCell(new TextBox { Text = parameter }));
                        break;
                }

                tableLayout.Rows.Add(row);
            }

            return tableLayout;
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
