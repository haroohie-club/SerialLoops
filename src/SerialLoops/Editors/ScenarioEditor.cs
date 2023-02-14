using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static HaruhiChokuretsuLib.Archive.Event.ScenarioCommand;

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

            IEnumerable<ListItem> verbs = Enum.GetNames<ScenarioVerb>().Select(v => new ListItem() { Text = v, Key = v });

            int commandIndex = 0;
            foreach ((ScenarioVerb verb, string parameter) in _scenario.ScenarioCommands)
            {
                TableRow row = new();

                ScenarioCommandDropDown commandDropDown = new() { CommandIndex = commandIndex, ModifyCommand = true };
                commandDropDown.Items.AddRange(verbs);
                commandDropDown.SelectedKey = verb.ToString();
                commandDropDown.SelectedKeyChanged += CommandDropDown_SelectedKeyChanged;
                row.Cells.Add(new TableCell(commandDropDown));
                switch (verb)
                {
                    case ScenarioVerb.LOAD_SCENE:
                        ScriptItem script = (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && i.DisplayName == parameter);

                        StackLayout scriptLink = ControlGenerator.GetFileLink(script, _tabs, _log);

                        ScenarioCommandDropDown scriptDropDown = new() { CommandIndex = commandIndex, ModifyCommand = false, Link = (ClearableLinkButton)scriptLink.Items[1].Control };
                        scriptDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                        scriptDropDown.SelectedKey = script.DisplayName;
                        scriptDropDown.SelectedKeyChanged += CommandDropDown_SelectedKeyChanged;

                        StackLayout scriptLayout = new()
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                scriptDropDown,
                                scriptLink,
                            },
                        };

                        row.Cells.Add(scriptLayout);
                        break;

                    case ScenarioVerb.PUZZLE_PHASE:
                        PuzzleItem puzzle = (PuzzleItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Puzzle && i.DisplayName == parameter);

                        DropDown puzzleDropDown = new();
                        puzzleDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Puzzle).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                        puzzleDropDown.SelectedKey = puzzle.DisplayName;
                        puzzleDropDown.SelectedKeyChanged += CommandDropDown_SelectedKeyChanged;

                        StackLayout puzzleLink = ControlGenerator.GetFileLink(puzzle, _tabs, _log);

                        StackLayout puzzleLayout = new()
                        {
                            Orientation = Orientation.Horizontal,
                            Items =
                            {
                                puzzleDropDown,
                                puzzleLink,
                            },
                        };

                        row.Cells.Add(puzzleLayout);
                        break;

                    default:
                        row.Cells.Add(new TableCell(new TextBox { Text = parameter }));
                        break;
                }

                tableLayout.Rows.Add(row);
                commandIndex++;
            }

            return new Scrollable { Content = tableLayout };
        }

        private void CommandDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScenarioCommandDropDown dropDown = (ScenarioCommandDropDown)sender;

            if (dropDown.ModifyCommand)
            {
                _scenario.Scenario.Commands[dropDown.CommandIndex].Verb = Enum.Parse<ScenarioVerb>(dropDown.SelectedKey);
            }
            else
            {
                ItemDescription item = _project.Items.First(i => i.Name == dropDown.SelectedKey);
                if (item.Type == ItemDescription.ItemType.Script)
                {
                    _scenario.Scenario.Commands[dropDown.CommandIndex].Parameter = ((ScriptItem)item).Event.Index;
                    _scenario.Refresh(_project);
                }
                else if (item.Type == ItemDescription.ItemType.Puzzle)
                {
                    _scenario.Scenario.Commands[dropDown.CommandIndex].Parameter = ((PuzzleItem)item).Puzzle.Index;
                    _scenario.Refresh(_project);
                }
                dropDown.Link.RemoveAllClickEvents();
                dropDown.Link.Text = item.DisplayName;
                dropDown.Link.ClickUnique += ControlGenerator.GetFileLinkClickHandler(item, _tabs, _log);
            }

            UpdateTabTitle(false);
        }
    }
}
