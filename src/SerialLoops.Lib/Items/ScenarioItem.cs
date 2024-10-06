using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items
{
    public class ScenarioItem : Item
    {
        public ScenarioStruct Scenario { get; set; }
        public List<(ScenarioCommand.ScenarioVerb Command, string Parameter)> ScenarioCommands { get; set; } = [];

        private IEnumerable<PuzzleItem> _puzzleItems;
        private IEnumerable<ScriptItem> _scriptItems;
        private IEnumerable<GroupSelectionItem> _groupSelectionItems;

        public ScenarioItem(ScenarioStruct scenario, Project project, ILogger log) : base("Scenario", ItemType.Scenario)
        {
            Scenario = scenario;
            CanRename = false;
            Refresh(project, log);
        }

        public override void Refresh(Project project, ILogger log)
        {
            _puzzleItems = project.Items.Where(i => i.Type == ItemType.Puzzle).Cast<PuzzleItem>();
            _scriptItems = project.Items.Where(i => i.Type == ItemType.Script).Cast<ScriptItem>();
            _groupSelectionItems = project.Items.Where(i => i.Type == ItemType.Group_Selection).Cast<GroupSelectionItem>();

            if (_groupSelectionItems.Any())
            {
                foreach (ScenarioCommand command in Scenario.Commands)
                {
                    ScenarioCommands.Add(GetCommandMacro(command));
                }
            }
        }

        public (ScenarioCommand.ScenarioVerb Command, string Parameter) GetCommandMacro(ScenarioCommand command)
        {
            switch (command.Verb)
            {
                case ScenarioCommand.ScenarioVerb.LOAD_SCENE:
                    ScriptItem script = _scriptItems.First(s => s.Event.Index == command.Parameter);
                    return (command.Verb, script.DisplayName);

                case ScenarioCommand.ScenarioVerb.PUZZLE_PHASE:
                    PuzzleItem puzzle = _puzzleItems.First(s => s.Puzzle.Index == command.Parameter);
                    return (command.Verb, puzzle.DisplayName);

                case ScenarioCommand.ScenarioVerb.ROUTE_SELECT:
                    GroupSelectionItem groupSelection = _groupSelectionItems.ElementAt(command.Parameter);
                    return (command.Verb, groupSelection.DisplayName);

                default:
                    return (command.Verb, command.Parameter.ToString());
            }
        }
    }
}
