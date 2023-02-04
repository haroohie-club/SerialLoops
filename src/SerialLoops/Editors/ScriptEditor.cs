using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using static SerialLoops.Lib.Script.ScriptCommand;

namespace SerialLoops.Editors
{
    public class ScriptEditor : Editor
    {
        private ScriptItem _script;

        public ScriptEditor(ScriptItem item, ILogger log) : base(item, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _script = (ScriptItem)Description;
            return new Scrollable
            {
                Content = GetCommandsContainer()
            };
        }

        private List<Lib.Script.ScriptCommand> GetEventCommands()
        {
            List<Lib.Script.ScriptCommand> commands = new();
            foreach (ScriptSection section in _script.Event.ScriptSections)
            {
                foreach (ScriptCommandInvocation command in section.Objects)
                {
                    commands.Add(Lib.Script.ScriptCommand.FromInvocation(command, _script.Event));
                }
            }
            return commands;
        }

        private Container GetCommandsContainer()
        {
            TableLayout layout = new()
            {
                Spacing = new Size(5, 5)
            };

            foreach (Lib.Script.ScriptCommand command in GetEventCommands())
            {
                TableRow row = new();

                DropDown commandDropDown = new();
                foreach (CommandVerb verb in Enum.GetValues(typeof(CommandVerb)))
                {
                    commandDropDown.Items.Add(new ListItem { Key = verb.ToString(), Text = verb.ToString() });
                }
                commandDropDown.SelectedKey = command.Verb.ToString();

                row.Cells.Add(new(commandDropDown));
                foreach (ScriptParameter parameter in command.Parameters) {
                    switch (parameter.Type)
                    {
                        case ScriptParameter.ParameterType.SHORT:
                            row.Cells.Add(new(new TextBox
                            {
                                Text = ((ShortScriptParameter)parameter).Value.ToString(),
                                PlaceholderText = ((ShortScriptParameter)parameter).Name
                            }));
                            break;
                        case ScriptParameter.ParameterType.STRING:
                            row.Cells.Add(new(new TextBox 
                            { 
                                Text = ((StringScriptParameter)parameter).Value,
                                PlaceholderText = ((StringScriptParameter)parameter).Name
                            }));
                            break;
                        case ScriptParameter.ParameterType.BOOL:
                            row.Cells.Add(new(ControlGenerator.GetControlWithLabel(parameter.Name, new CheckBox() 
                            { 
                                Checked = ((BoolScriptParameter)parameter).Value
                            }
                            )));
                            break;
                    }
                }

                layout.Rows.Add(row);
            }

            return layout;
        }
    }
}
