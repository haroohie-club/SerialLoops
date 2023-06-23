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
        private StackLayout _editorControls;
        private ScenarioCommandListPanel _commandsPanel;

        private Button _addButton;
        private Button _deleteButton;
        private Button _clearButton;

        private bool _triggerRefresh = true;

        private readonly IEnumerable<ListItem> verbs = Enum.GetNames<ScenarioVerb>().Select(v => new ListItem() { Text = v, Key = v });

        public ScenarioEditor(ScenarioItem item, ILogger log, Project project, EditorTabsPanel tabs) : base(item, log, project, tabs)
        {
        }

        public override Container GetEditorPanel()
        {
            _scenario = (ScenarioItem)Description;
            return GetCommandsContainer();
        }

        private Container GetCommandsContainer()
        {
            TableLayout layout = new() { Spacing = new Size(5, 5) };

            _commandsPanel = new(_scenario.ScenarioCommands, new Size(280, 185), _log);
            ListBox commandsList = _commandsPanel.Viewer;
            commandsList.SelectedIndexChanged += CommandsPanel_SelectedItemChanged;
            _editorControls = new() 
            { 
                Orientation = Orientation.Vertical,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = 10,
                Spacing = 5
            };

            TableRow mainRow = new();
            mainRow.Cells.Add(new TableLayout(GetEditorButtons(), _commandsPanel));
            mainRow.Cells.Add(new(new Scrollable { Content = _editorControls }));

            layout.Rows.Add(mainRow);
            return layout;
        }

        private StackLayout GetEditorButtons()
        {
            _addButton = new()
            {
                Image = ControlGenerator.GetIcon("Add", _log),
                ToolTip = "Add Command",
                Width = 22
            };
            _addButton.Click += AddButton_Click;

            _deleteButton = new()
            {
                Image = ControlGenerator.GetIcon("Remove", _log),
                ToolTip = "Remove Command",
                Width = 22,
                Enabled = _commandsPanel.SelectedCommand is not null
            };
            _deleteButton.Click += DeleteButton_Click;

            _clearButton = new()
            {
                Image = ControlGenerator.GetIcon("Clear", _log),
                ToolTip = "Clear Scenario",
                Width = 22
            };
            _clearButton.Click += ClearButton_Click;

            return new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Width = _commandsPanel.Width,
                Spacing = 5,
                Padding = 5,
                Items = { _addButton, _deleteButton, _clearButton }
            };
        }

        private void CommandsPanel_SelectedItemChanged(object sender, EventArgs e)
        {
            if (!_triggerRefresh)
            {
                return;
            }

            _triggerRefresh = false;
            RefreshCommands();
            _triggerRefresh = true;

            var command = _commandsPanel.SelectedCommand;
            int commandIndex = _commandsPanel.Viewer.SelectedIndex;
            _editorControls.Items.Clear();

            _addButton.Enabled = true;
            _deleteButton.Enabled = true;

            if (command is null)
            {
                return;
            }

            ScenarioCommandDropDown commandDropDown = new() { CommandIndex = commandIndex, ModifyCommand = true };
            commandDropDown.Items.AddRange(verbs);
            commandDropDown.SelectedKey = command.Value.Verb.ToString();
            commandDropDown.SelectedKeyChanged += CommandDropDown_SelectedKeyChanged;
            _editorControls.Items.Add(ControlGenerator.GetControlWithLabel("Command", commandDropDown));

            Item parameterItem = null;
            StackLayout parameterLink = null;
            ScenarioCommandDropDown parameterDropDown = new() { CommandIndex = commandIndex, ModifyCommand = false };

            switch (command.Value.Verb)
            {
                case ScenarioVerb.LOAD_SCENE:
                    parameterItem = (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && i.DisplayName == command.Value.Parameter);
                    parameterDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                    break;

                case ScenarioVerb.PUZZLE_PHASE:
                    parameterItem = (PuzzleItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Puzzle && i.DisplayName == command.Value.Parameter);
                    parameterDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Puzzle).Select(p => new ListItem { Key = p.DisplayName, Text = p.DisplayName }));
                    break;

                case ScenarioVerb.ROUTE_SELECT:
                    parameterItem = (GroupSelectionItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Group_Selection && ((GroupSelectionItem)i).Index == short.Parse(command.Value.Parameter));
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
                    Items = { parameterDropDown, parameterLink }
                };
                commandDropDown.ParameterLayout = parameterLayout;
                _editorControls.Items.Add(ControlGenerator.GetControlWithLabel(parameterItem.Type.ToString().Replace("_", " "), parameterLayout));
            }
            else
            {
                ScenarioCommandTextBox parameterBox = new() { Text = command.Value.Parameter, CommandIndex = commandIndex };
                parameterBox.TextChanged += ParameterBox_TextChanged;
                StackLayout parameterLayout = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Items = { parameterBox }
                };
                commandDropDown.ParameterLayout = parameterLayout;
                _editorControls.Items.Add(ControlGenerator.GetControlWithLabel($"Parameter Value", parameterLayout));
            }
        }

        private void CommandDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScenarioCommandDropDown dropDown = (ScenarioCommandDropDown)sender;
            if (string.IsNullOrEmpty(dropDown.SelectedKey)) return;

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

                dropDown.Link.RemoveAllClickEvents();
                dropDown.Link.Text = item.DisplayName;
                dropDown.Link.ClickUnique += ControlGenerator.GetFileLinkClickHandler(item, _tabs, _log);
            }

            UpdateTabTitle(false, dropDown);
        }

        private void ParameterBox_TextChanged(object sender, EventArgs e)
        {
            ScenarioCommandTextBox parameterBox = (ScenarioCommandTextBox)sender;
            if (short.TryParse(parameterBox.Text, out short parameter))
            {
                _scenario.Scenario.Commands[parameterBox.CommandIndex].Parameter = parameter;
                _scenario.ScenarioCommands[parameterBox.CommandIndex] = (_scenario.ScenarioCommands[parameterBox.CommandIndex].Command, parameter.ToString());

                UpdateTabTitle(false, parameterBox);
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = Math.Min(_scenario.Scenario.Commands.Count - 1, _commandsPanel.Viewer.SelectedIndex);
            _scenario.Scenario.Commands.Insert(selectedIndex + 1, new(ScenarioVerb.LOAD_SCENE, 1));
            _scenario.ScenarioCommands.Insert(selectedIndex + 1, (ScenarioVerb.LOAD_SCENE, "BGTEST"));

            RefreshCommands();
            UpdateTabTitle(false);
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = _commandsPanel.Viewer.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _scenario.Scenario.Commands.Count) return;

            _scenario.Scenario.Commands.RemoveAt(selectedIndex);
            _scenario.ScenarioCommands.RemoveAt(selectedIndex);

            RefreshCommands();
            UpdateTabTitle(false);
        }
        
        private void ClearButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Clear all commands from the game scenario?\nThis action is irreversible.",
                "Clear Scenario",
                MessageBoxButtons.OKCancel,
                MessageBoxType.Warning
            );
            if (result != DialogResult.Ok) return;

            _scenario.Scenario.Commands.Clear();
            _scenario.ScenarioCommands.Clear();
            _scenario.Scenario.Commands.Add(new(ScenarioVerb.NEW_GAME, 1));
            _scenario.ScenarioCommands.Add((ScenarioVerb.NEW_GAME, "1"));

            RefreshCommands();
            UpdateTabTitle(false);
        }

        private void RefreshCommands()
        {
            _commandsPanel.Commands = _scenario.ScenarioCommands;
            _deleteButton.Enabled = _commandsPanel.SelectedCommand is not null;
        }
    }
}
