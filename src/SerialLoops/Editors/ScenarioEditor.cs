using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using System.Linq;

namespace SerialLoops.Editors
{
    public class ScenarioEditor : Editor
    {
        private ScenarioItem _scenario;

        public ScenarioEditor(ScenarioItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            _scenario = (ScenarioItem)Description;
            StackLayout mainLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 20,
            };

            string[] verbs = new string[]
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
            };

            foreach (ScenarioCommand command in _scenario.Scenario.Commands)
            {
                DropDown commandDropDown = new();
                commandDropDown.Items.AddRange(verbs.Select(v => new ListItem() { Text = v }));
                switch (command.Verb)
                {
                    default:

                        mainLayout.Items.Add(new Label { Text = "" })
                        break;
                }
            }

            return mainLayout;
        }
    }
}
