using Eto.Drawing;
using Eto.Forms;
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

                Item parameterItem = null;
                StackLayout parameterLink = null;
                ScenarioCommandDropDown parameterDropDown = new() { CommandIndex = commandIndex, ModifyCommand = false };

                switch (verb)
                {
                    case ScenarioVerb.LOAD_SCENE:
                        parameterItem = (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && i.DisplayName == parameter);
                        parameterDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                        break;

                    case ScenarioVerb.PUZZLE_PHASE:
                        parameterItem = (PuzzleItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Puzzle && i.DisplayName == parameter);
                        parameterDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Puzzle).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                        break;

                    case ScenarioVerb.ROUTE_SELECT:
                        parameterItem = (GroupSelectionItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Group_Selection && ((GroupSelectionItem)i).Index == short.Parse(parameter));
                        parameterDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Group_Selection).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                        break;
                }
                
                if (parameterItem is not null)
                {
                    parameterDropDown.SelectedKey = parameterItem.DisplayName;
                    parameterDropDown.SelectedKeyChanged += CommandDropDown_SelectedKeyChanged;
                    parameterLink = ControlGenerator.GetFileLink(parameterItem, _tabs, _log);
                    parameterDropDown.Link = (ClearableLinkButton)parameterLink.Items[1].Control;
                    StackLayout parameterLayout = new()
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items =
                        {
                            parameterDropDown,
                            parameterLink,
                        },
                    };
                    commandDropDown.ParameterLayout = parameterLayout;
                    row.Cells.Add(parameterLayout);
                }
                else
                {
                    ScenarioCommandTextBox parameterBox = new() { Text = parameter, CommandIndex = commandIndex };
                    parameterBox.TextChanged += ParameterBox_TextChanged;
                    StackLayout parameterLayout = new()
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items =
                        {
                            parameterBox
                        },
                    };
                    commandDropDown.ParameterLayout = parameterLayout;
                    row.Cells.Add(parameterLayout);
                }

                tableLayout.Rows.Add(row);
                commandIndex++;
            }

            return new Scrollable { Content = tableLayout };
        }

        private void CommandDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScenarioCommandDropDown dropDown = (ScenarioCommandDropDown)sender;
            if (string.IsNullOrEmpty(dropDown.SelectedKey))
            {
                return;
            }

            if (dropDown.ModifyCommand)
            {
                _scenario.Scenario.Commands[dropDown.CommandIndex].Verb = Enum.Parse<ScenarioVerb>(dropDown.SelectedKey);
                _scenario.ScenarioCommands[dropDown.CommandIndex] = (_scenario.Scenario.Commands[dropDown.CommandIndex].Verb, _scenario.ScenarioCommands[dropDown.CommandIndex].Parameter);
                dropDown.ParameterLayout.Items.Clear();

                ScenarioCommandDropDown parameterDropDown = new() { CommandIndex = dropDown.CommandIndex, ModifyCommand = false };
                switch (Enum.Parse<ScenarioVerb>(dropDown.SelectedKey))
                {
                    case ScenarioVerb.LOAD_SCENE:
                        parameterDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                        break;

                    case ScenarioVerb.PUZZLE_PHASE:
                        parameterDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Puzzle).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                        break;

                    case ScenarioVerb.ROUTE_SELECT:
                        parameterDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Group_Selection).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                        break;
                }
                if (parameterDropDown.Items.Count > 0)
                {
                    parameterDropDown.SelectedKeyChanged += CommandDropDown_SelectedKeyChanged;
                    StackLayout parameterLink = ControlGenerator.GetFileLink(_project.Items.First(i => i.DisplayName == parameterDropDown.Items.First().Key), _tabs, _log);
                    parameterDropDown.Link = (ClearableLinkButton)parameterLink.Items[1].Control;
                    parameterDropDown.SelectedKey = parameterDropDown.Items.First().Key;

                    dropDown.ParameterLayout.Items.Add(parameterDropDown);
                    dropDown.ParameterLayout.Items.Add(parameterLink);
                }
                else
                {
                    ScenarioCommandTextBox parameterBox = new() { CommandIndex = dropDown.CommandIndex };
                    parameterBox.TextChanged += ParameterBox_TextChanged;
                    dropDown.ParameterLayout.Items.Add(parameterBox);
                }

                _scenario.Refresh(_project);
            }
            else
            {
                ItemDescription item = _project.Items.First(i => i.Name == dropDown.SelectedKey);
                switch (item.Type)
                {
                    case ItemDescription.ItemType.Group_Selection:
                        _scenario.Scenario.Commands[dropDown.CommandIndex].Parameter = ((GroupSelectionItem)item).Index;
                        _scenario.ScenarioCommands[dropDown.CommandIndex] = (_scenario.ScenarioCommands[dropDown.CommandIndex].Command, ((GroupSelectionItem)item).Index.ToString());
                        break;

                    case ItemDescription.ItemType.Puzzle:
                        _scenario.Scenario.Commands[dropDown.CommandIndex].Parameter = ((PuzzleItem)item).Puzzle.Index;
                        _scenario.ScenarioCommands[dropDown.CommandIndex] = (_scenario.ScenarioCommands[dropDown.CommandIndex].Command, item.DisplayName);
                        break;

                    case ItemDescription.ItemType.Script:
                        _scenario.Scenario.Commands[dropDown.CommandIndex].Parameter = ((ScriptItem)item).Event.Index;
                        _scenario.ScenarioCommands[dropDown.CommandIndex] = (_scenario.ScenarioCommands[dropDown.CommandIndex].Command, item.DisplayName);
                        break;
                }
                _scenario.Refresh(_project);

                dropDown.Link.RemoveAllClickEvents();
                dropDown.Link.Text = item.DisplayName;
                dropDown.Link.ClickUnique += ControlGenerator.GetFileLinkClickHandler(item, _tabs, _log);
            }

            UpdateTabTitle(false);
        }

        private void ParameterBox_TextChanged(object sender, EventArgs e)
        {
            ScenarioCommandTextBox parameterBox = (ScenarioCommandTextBox)sender;
            if (short.TryParse(parameterBox.Text, out short parameter))
            {
                _scenario.Scenario.Commands[parameterBox.CommandIndex].Parameter = parameter;
                _scenario.ScenarioCommands[parameterBox.CommandIndex] = (_scenario.ScenarioCommands[parameterBox.CommandIndex].Command, parameter.ToString());

                UpdateTabTitle(false);
            }
        }
    }
}
