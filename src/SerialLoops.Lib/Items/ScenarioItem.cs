﻿using HaruhiChokuretsuLib.Archive.Event;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class ScenarioItem : Item
    {
        public ScenarioStruct Scenario { get; set; }
        public List<(string Command, string Parameter)> ScenarioCommands { get; set; } = new();

        public ScenarioItem(ScenarioStruct scenario, Project project) : base("Scenario", ItemType.Scenario)
        {
            Scenario = scenario;
            Refresh(project);
        }

        public override void Refresh(Project project)
        {
            IEnumerable<PuzzleItem> puzzleItems = project.Items.Where(i => i.Type == ItemType.Puzzle).Cast<PuzzleItem>().ToList();
            IEnumerable<ScriptItem> scriptItems = project.Items.Where(i => i.Type == ItemType.Script).Cast<ScriptItem>().ToList();

            foreach (ScenarioCommand command in Scenario.Commands)
            {
                switch (command.Verb)
                {
                    case "LOAD_SCENE":
                        ScriptItem script = scriptItems.First(s => s.Event.Index == command.Parameter);
                        ScenarioCommands.Add((command.Verb, script.Name));
                        break;

                    case "PUZZLE_PHASE":
                        PuzzleItem puzzle = puzzleItems.First(s => s.Puzzle.Index == command.Parameter);
                        ScenarioCommands.Add((command.Verb, puzzle.Name));
                        break;

                    default:
                        ScenarioCommands.Add((command.Verb, command.Parameter.ToString()));
                        break;
                }
            }
        }
    }
}
