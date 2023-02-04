using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static SerialLoops.Lib.Script.ScriptItemCommand;

namespace SerialLoops.Editors
{
    public class ScriptEditor : Editor
    {
        private ScriptItem _script;
        private List<ScriptItemCommand> _commands = new();

        private TableLayout _detailsLayout = new();
        private StackLayout _preview = new() { Items = { new SKGuiImage(new(256, 384)) } };
        private StackLayout _editorControls = new();

        public ScriptEditor(ScriptItem item, Project project, ILogger log) : base(item, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _script = (ScriptItem)Description;
            PopulateScriptCommands(_project);
            return GetCommandsContainer(_project);
        }

        private void PopulateScriptCommands(Project project)
        {
            foreach (ScriptSection section in _script.Event.ScriptSections)
            {
                foreach (ScriptCommandInvocation command in section.Objects)
                {
                    _commands.Add(FromInvocation(command, _script.Event, project));
                }
            }
        }

        private Container GetCommandsContainer(Project project)
        {
            TableLayout layout = new()
            {
                Spacing = new Size(5, 5),
            };

            TableRow mainRow = new();

            ListBox commandListBox = new();
            commandListBox.Items.AddRange(_commands.Select(i => new ListItem() { Text = i.ToString() }));
            commandListBox.SelectedIndexChanged += CommandListBox_SelectedIndexChanged;
            mainRow.Cells.Add(commandListBox);

            _detailsLayout = new()
            {
                Spacing = new Size(5, 5),
            };

            _editorControls = new()
            {
                Orientation = Orientation.Horizontal
            };
            _detailsLayout.Rows.Add(new(_preview));
            _detailsLayout.Rows.Add(new(_editorControls));

            mainRow.Cells.Add(new(_detailsLayout));
            layout.Rows.Add(mainRow);

            return layout;
        }

        private void CommandListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ScriptItemCommand command = _commands[((ListBox)sender).SelectedIndex];
            _editorControls.Items.Clear();

            if (command.Parameters.Count == 0)
            {
                return;
            }

            int cols = (int)Math.Round(Math.Sqrt(command.Parameters.Count));

            // We're going to embed table layouts inside a table layout so we can have colspan
            TableLayout controlsTable = new()
            {
                Spacing = new Size(5, 5)
            };
            controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows.Add(new());

            int currentRow = 0, currentCol = 0;

            foreach (ScriptParameter parameter in command.Parameters)
            {
                switch (parameter.Type)
                {
                    case ScriptParameter.ParameterType.BG:
                        DropDown bgDropDown = new();
                        bgDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        bgDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        bgDropDown.SelectedKey = ((BgScriptParameter)parameter).Background?.Name ?? "NONE";

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgDropDown));
                        break;

                    case ScriptParameter.ParameterType.BG_SCROLL_DIRECTION:
                        DropDown bgScrollDropDown = new();
                        bgScrollDropDown.Items.AddRange(Enum.GetValues<BgScrollDirectionScriptParameter.BgScrollDirection>().Select(i => new ListItem { Text = i.ToString(), Key = i.ToString() }));
                        bgScrollDropDown.SelectedKey = ((BgScrollDirectionScriptParameter)parameter).ScrollDirection.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgScrollDropDown));
                        break;

                    case ScriptParameter.ParameterType.BGM_MODE:
                        DropDown bgmModeDropDown = new();
                        bgmModeDropDown.Items.AddRange(Enum.GetValues<BgmModeScriptParameter.BgmMode>().Select(i => new ListItem { Text = i.ToString(), Key = i.ToString() }));
                        bgmModeDropDown.SelectedKey = ((BgmModeScriptParameter)parameter).Mode.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgmModeDropDown));
                        break;

                    case ScriptParameter.ParameterType.BOOL:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new CheckBox { Checked = ((BoolScriptParameter)parameter).Value }));
                        break;

                    case ScriptParameter.ParameterType.CHESS_FILE:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((ChessFileScriptParameter)parameter).ChessFileIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.CHESS_PIECE:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((ChessPieceScriptParameter)parameter).ChessPiece.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.CHESS_SPACE:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((ChessSpaceScriptParameter)parameter).SpaceIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.CHIBI:
                        DropDown chibiDropDown = new();
                        chibiDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        chibiDropDown.SelectedKey = ((ChibiScriptParameter)parameter).Chibi.Name;
                        break;

                    case ScriptParameter.ParameterType.CHIBI_EMOTE:
                        DropDown chibiEmoteDropDown = new();
                        chibiEmoteDropDown.Items.AddRange(Enum.GetValues<ChibiEmoteScriptParameter.ChibiEmote>().Select(i => new ListItem { Text = i.ToString(), Key = i.ToString() }));
                        chibiEmoteDropDown.SelectedKey = ((ChibiEmoteScriptParameter)parameter).Emote.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, chibiEmoteDropDown));
                        break;

                    case ScriptParameter.ParameterType.CHIBI_ENTER_EXIT:
                        DropDown chibiEnterExitDropDown = new();
                        chibiEnterExitDropDown.Items.AddRange(Enum.GetValues<ChibiEnterExitScriptParameter.ChibiEnterExitType>().Select(i => new ListItem { Text = i.ToString(), Key = i.ToString() }));
                        chibiEnterExitDropDown.SelectedKey = ((ChibiEnterExitScriptParameter)parameter).Mode.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, chibiEnterExitDropDown));
                        break;

                    case ScriptParameter.ParameterType.COLOR:
                        ColorPicker colorPicker = new()
                        {
                            AllowAlpha = false,
                            Value = ((ColorScriptParameter)parameter).Color.ToEtoDrawingColor(),
                        };

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, colorPicker));
                        break;

                    case ScriptParameter.ParameterType.CONDITIONAL:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((ConditionalScriptParameter)parameter).Value } ));
                        break;

                    case ScriptParameter.ParameterType.COLOR_MONOCHROME:
                        DropDown colorMonochromeDropDown = new();
                        colorMonochromeDropDown.Items.AddRange(Enum.GetValues<ColorMonochromeScriptParameter.ColorMonochrome>().Select(i => new ListItem { Text = i.ToString(), Key = i.ToString() }));
                        colorMonochromeDropDown.SelectedKey = ((ColorMonochromeScriptParameter)parameter).ColorType.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, colorMonochromeDropDown));
                        break;

                    case ScriptParameter.ParameterType.DIALOGUE:
                        DialogueScriptParameter dialogueParam = (DialogueScriptParameter)parameter;
                        DropDown speakerDropDown = new();
                        speakerDropDown.Items.AddRange(Enum.GetValues<Speaker>().Select(s => new ListItem { Text = s.ToString(), Key = s.ToString() }));
                        speakerDropDown.SelectedKey = dialogueParam.Line.Speaker.ToString();
                        if (currentCol > 0)
                        {
                            controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                            currentRow++;
                            currentCol = 0;
                        }
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabelTable(parameter.Name,
                            new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Items =
                                {
                                    speakerDropDown,
                                    new StackLayoutItem(new TextArea { Text = dialogueParam.Line.Text, AcceptsReturn = true }, expand: true),
                                },
                            }));
                        controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows.Add(new());
                        currentRow++;
                        currentCol = 0;
                        break;

                    case ScriptParameter.ParameterType.DIALOGUE_PROPERTY:
                        DropDown dialoguePropertyDropDown = new();
                        dialoguePropertyDropDown.Items.AddRange(Enum.GetValues<Speaker>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        dialoguePropertyDropDown.SelectedKey = ((DialoguePropertyScriptParameter)parameter).DialogueProperties.Character.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, dialoguePropertyDropDown));
                        break;

                    case ScriptParameter.ParameterType.EPISODE_HEADER:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((EpisodeHeaderScriptParameter)parameter).EpisodeHeaderIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.FLAG:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((FlagScriptParameter)parameter).FlagName }));
                        break;

                    case ScriptParameter.ParameterType.SHORT:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((ShortScriptParameter)parameter).Value.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.SPRITE:
                        DropDown spriteDropDown = new();
                        spriteDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        spriteDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Character_Sprite).Select(s => new ListItem { Text = s.Name, Key = s.Name }));
                        spriteDropDown.SelectedKey = ((SpriteScriptParameter)parameter).Sprite.Name;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, spriteDropDown));
                        break;

                    case ScriptParameter.ParameterType.SPRITE_ENTRANCE:
                        DropDown spriteEntranceDropDown = new();
                        spriteEntranceDropDown.Items.AddRange(Enum.GetValues<SpriteEntranceScriptParameter.SpriteEntranceTransition>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        spriteEntranceDropDown.SelectedKey = ((SpriteEntranceScriptParameter)parameter).EntranceTransition.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, spriteEntranceDropDown));
                        break;

                    case ScriptParameter.ParameterType.SPRITE_EXIT:
                        DropDown spriteExitDropDown = new();
                        spriteExitDropDown.Items.AddRange(Enum.GetValues<SpriteExitScriptParameter.SpriteExitTransition>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        spriteExitDropDown.SelectedKey = ((SpriteExitScriptParameter)parameter).ExitTransition.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, spriteExitDropDown));
                        break;

                    case ScriptParameter.ParameterType.SPRITE_SHAKE:
                        DropDown spriteShakeDropDown = new();
                        spriteShakeDropDown.Items.AddRange(Enum.GetValues<SpriteShakeScriptParameter.SpriteShakeEffect>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        spriteShakeDropDown.SelectedKey = ((SpriteShakeScriptParameter)parameter).ShakeEffect.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, spriteShakeDropDown));
                        break;

                    case ScriptParameter.ParameterType.TEXT_ENTRANCE_EFFECT:
                        DropDown textEntranceEffectDropDown = new();
                        textEntranceEffectDropDown.Items.AddRange(Enum.GetValues<TextEntranceEffectScriptParameter.TextEntranceEffect>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        textEntranceEffectDropDown.SelectedKey = ((TextEntranceEffectScriptParameter)parameter).EntranceEffect.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, textEntranceEffectDropDown));
                        break;

                    case ScriptParameter.ParameterType.VOICE_LINE:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, 
                            new TextBox { Text = ((VoiceLineScriptParameter)parameter).VoiceIndex.ToString() }));
                        break;

                    default:
                        _log.LogError($"Invalid parameter detected in script {_script.Name} parameter {parameter.Name}");
                        break;
                }
                currentCol++;
                if (currentCol >= cols)
                {
                    currentCol = 0;
                    currentRow++;
                    controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                    ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows.Add(new());
                }
            }

            _editorControls.Items.Add(new StackLayoutItem(controlsTable, expand: true));
        }
    }
}
