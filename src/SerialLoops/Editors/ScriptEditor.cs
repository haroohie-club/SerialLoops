using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using static SerialLoops.Lib.Script.ScriptItemCommand;

namespace SerialLoops.Editors
{
    public class ScriptEditor : Editor
    {
        private ScriptItem _script;
        private Dictionary<ScriptSection, List<ScriptItemCommand>> _commands = new();

        private TableLayout _detailsLayout = new();
        private StackLayout _preview = new() { Items = { new SKGuiImage(new(256, 384)) } };
        private StackLayout _editorControls = new();
        private ScriptCommandListPanel _commandsPanel;

        public ScriptEditor(ScriptItem item, Project project, ILogger log, EditorTabsPanel tabs) : base(item, log, project, tabs)
        {
        }

        public override Container GetEditorPanel()
        {
            _script = (ScriptItem)Description;
            PopulateScriptCommands();
            _script.CalculateGraphEdges(_commands);
            return GetCommandsContainer();
        }

        private void PopulateScriptCommands()
        {
            foreach (ScriptSection section in _script.Event.ScriptSections)
            {
                _commands.Add(section, new());
                foreach (ScriptCommandInvocation command in section.Objects)
                {
                    _commands[section].Add(FromInvocation(command, section, _commands[section].Count, _script.Event, _project));
                }
            }
        }

        private Container GetCommandsContainer()
        {
            TableLayout layout = new()
            {
                Spacing = new Size(5, 5),
            };

            TableRow mainRow = new();

            _commandsPanel = new(_commands, new Size(280, 185), expandItems: true, _log);
            _commandsPanel.Viewer.SelectedItemChanged += CommandsPanel_SelectedIndexChanged;
            mainRow.Cells.Add(_commandsPanel);

            _detailsLayout = new()
            {
                Spacing = new Size(5, 5),
            };

            _editorControls = new()
            {
                Orientation = Orientation.Horizontal
            };
            _detailsLayout.Rows.Add(new(_preview));
            _detailsLayout.Rows.Add(new(new Scrollable { Content = _editorControls }));

            mainRow.Cells.Add(new(_detailsLayout));
            layout.Rows.Add(mainRow);

            return layout;
        }

        private void CommandsPanel_SelectedIndexChanged(object sender, EventArgs e)
        {
            ScriptItemCommand command = ((ScriptCommandSectionEntry)((ScriptCommandSectionTreeGridView)sender).SelectedItem).Command;
            _editorControls.Items.Clear();
            if (command is null) // if we've selected a script section header
            {
                return;
            }

            Application.Instance.Invoke(() => UpdatePreview());

            if (command.Parameters.Count == 0)
            {
                return;
            }

            int cols = 1;

            // We're going to embed table layouts inside a table layout so we can have colspan
            TableLayout controlsTable = new()
            {
                Spacing = new Size(5, 5)
            };
            controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows.Add(new());

            int currentRow = 0, currentCol = 0;
            for (int i = 0; i < command.Parameters.Count; i++)
            {
                ScriptParameter parameter = command.Parameters[i];
                switch (parameter.Type)
                {
                    case ScriptParameter.ParameterType.BG:
                        BgScriptParameter bgParam = (BgScriptParameter)parameter;
                        CommandGraphicSelectionButton bgSelectionButton = new(bgParam.Background is not null ? bgParam.Background
                            : NonePreviewableGraphic.BACKGROUND, _tabs, _log)
                        {
                            Command = command,
                            ParameterIndex = i,
                            Project = _project,
                        };
                        bgSelectionButton.Items.Add(NonePreviewableGraphic.BACKGROUND);
                        bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background && (!bgParam.Kinetic ^ ((BackgroundItem)i).BackgroundType == BgType.KINETIC_SCREEN)).Select(i => (IPreviewableGraphic)i));
                        bgSelectionButton.SelectedChanged.Executed += (obj, args) => BgSelectionButton_SelectionMade(bgSelectionButton, args);

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgSelectionButton));
                        break;

                    case ScriptParameter.ParameterType.BG_SCROLL_DIRECTION:
                        DropDown bgScrollDropDown = new();
                        bgScrollDropDown.Items.AddRange(Enum.GetValues<BgScrollDirectionScriptParameter.BgScrollDirection>().Select(i => new ListItem { Text = i.ToString(), Key = i.ToString() }));
                        bgScrollDropDown.SelectedKey = ((BgScrollDirectionScriptParameter)parameter).ScrollDirection.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgScrollDropDown));
                        break;

                    case ScriptParameter.ParameterType.BGM:
                        BgmScriptParameter bgmParam = (BgmScriptParameter)parameter;
                        StackLayout bgmLink = ControlGenerator.GetFileLink(bgmParam.Bgm, _tabs, _log);

                        CommandDropDown bgmDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)bgmLink.Items[1].Control };
                        bgmDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.BGM).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        bgmDropDown.SelectedKey = bgmParam.Bgm.Name;
                        bgmDropDown.SelectedKeyChanged += BgmDropDown_SelectedKeyChanged;                        

                        StackLayout bgmLayout = new()
                        {
                            Orientation = Orientation.Horizontal,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Items =
                            {
                                ControlGenerator.GetControlWithLabel(parameter.Name, bgmDropDown),
                                bgmLink,
                            }
                        };

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(bgmLayout);
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
                        CommandDropDown chibiDropDown = new() { Command = command, ParameterIndex = i };
                        chibiDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        chibiDropDown.SelectedKey = ((ChibiScriptParameter)parameter).Chibi.Name;
                        chibiDropDown.SelectedKeyChanged += ChibiDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, chibiDropDown));
                        break;

                    case ScriptParameter.ParameterType.CHIBI_EMOTE:
                        DropDown chibiEmoteDropDown = new();
                        chibiEmoteDropDown.Items.AddRange(Enum.GetValues<ChibiEmoteScriptParameter.ChibiEmote>().Select(i => new ListItem { Text = i.ToString(), Key = i.ToString() }));
                        chibiEmoteDropDown.SelectedKey = ((ChibiEmoteScriptParameter)parameter).Emote.ToString();
                        chibiEmoteDropDown.SelectedKeyChanged += ChibiEmoteDropDown_SelectedKeyChanged;

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
                            new TextBox { Text = ((ConditionalScriptParameter)parameter).Value }));
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

                    case ScriptParameter.ParameterType.ITEM:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((ItemScriptParameter)parameter).ItemIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.MAP:
                        MapScriptParameter mapParam = (MapScriptParameter)parameter;
                        DropDown mapDropDown = new();
                        mapDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        mapDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Map).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        mapDropDown.SelectedKey = mapParam.Map?.Name ?? "NONE";

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, mapDropDown));
                        break;

                    case ScriptParameter.ParameterType.OPTION:
                        OptionScriptParameter optionParam = (OptionScriptParameter)parameter;
                        DropDown optionScriptSectionDropDown = new();
                        optionScriptSectionDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        optionScriptSectionDropDown.Items.AddRange(_script.Event.ScriptSections.Skip(1).Select(s => new ListItem { Text = s.Name, Key = s.Name }));
                        optionScriptSectionDropDown.SelectedKey = optionParam.Option.Id == 0 ? "NONE" : _script.Event.LabelsSection.Objects.First(l => l.Id == optionParam.Option.Id).Name.Replace("/", "");

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Items =
                                {
                                    optionScriptSectionDropDown,
                                    new StackLayoutItem(new TextBox { Text = optionParam.Option.Text }, expand: true),
                                }
                            }));
                        break;

                    case ScriptParameter.ParameterType.PALETTE_EFFECT:
                        DropDown paletteEffectDropDown = new();
                        paletteEffectDropDown.Items.AddRange(Enum.GetValues<PaletteEffectScriptParameter.PaletteEffect>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        paletteEffectDropDown.SelectedKey = ((PaletteEffectScriptParameter)parameter).Effect.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, paletteEffectDropDown));
                        break;

                    case ScriptParameter.ParameterType.PLACE:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((PlaceScriptParameter)parameter).PlaceIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.SCREEN:
                        DropDown screenDropDown = new();
                        screenDropDown.Items.AddRange(Enum.GetValues<ScreenScriptParameter.DsScreen>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        screenDropDown.SelectedKey = ((ScreenScriptParameter)parameter).Screen.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, screenDropDown));
                        break;

                    case ScriptParameter.ParameterType.SCRIPT_SECTION:
                        DropDown scriptSectionDropDown = new();
                        scriptSectionDropDown.Items.AddRange(_script.Event.ScriptSections.Select(s => new ListItem { Text = s.Name, Key = s.Name }));
                        scriptSectionDropDown.SelectedKey = ((ScriptSectionScriptParameter)parameter)?.Section?.Name ?? "NONE";

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, scriptSectionDropDown));
                        break;

                    case ScriptParameter.ParameterType.SFX:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((SfxScriptParameter)parameter).SfxIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.SFX_MODE:
                        DropDown sfxModeDropDown = new();
                        sfxModeDropDown.Items.AddRange(Enum.GetValues<SfxModeScriptParameter.SfxMode>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        sfxModeDropDown.SelectedKey = ((SfxModeScriptParameter)parameter).Mode.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, sfxModeDropDown));
                        break;

                    case ScriptParameter.ParameterType.SHORT:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((ShortScriptParameter)parameter).Value.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.SPRITE:
                        SpriteScriptParameter spriteParam = (SpriteScriptParameter)parameter;
                        CommandGraphicSelectionButton spriteSelectionButton = new(spriteParam.Sprite is not null ? spriteParam.Sprite
                            : NonePreviewableGraphic.CHARACTER_SPRITE, _tabs, _log)
                        {
                            Command = command,
                            ParameterIndex = i,
                            Project = _project,
                        };
                        spriteSelectionButton.Items.Add(NonePreviewableGraphic.CHARACTER_SPRITE);
                        spriteSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Character_Sprite).Select(s => (IPreviewableGraphic)s));
                        spriteSelectionButton.SelectedChanged.Executed += (obj, args) => SpriteSelectionButton_SelectionMade(spriteSelectionButton, args);
                        
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, spriteSelectionButton));
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

                    case ScriptParameter.ParameterType.TOPIC:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((TopicScriptParameter)parameter).TopicId.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.TRANSITION:
                        DropDown transitionDropDown = new();
                        transitionDropDown.Items.AddRange(Enum.GetValues<TransitionScriptParameter.TransitionEffect>().Select(t => new ListItem { Text = t.ToString(), Key = t.ToString() }));
                        transitionDropDown.SelectedKey = ((TransitionScriptParameter)parameter).Transition.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, transitionDropDown));
                        break;

                    case ScriptParameter.ParameterType.VOICE_LINE:
                        VoicedLineScriptParameter vceParam = (VoicedLineScriptParameter)parameter;
                        StackLayout vceLink = ControlGenerator.GetFileLink(vceParam.VoiceLine is not null ? vceParam.VoiceLine : NoneItem.VOICE, _tabs, _log);

                        CommandDropDown vceDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)vceLink.Items[1].Control };
                        vceDropDown.Items.Add(new ListItem { Key = "NONE", Text = "NONE" });
                        vceDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Voice).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        vceDropDown.SelectedKey = vceParam.VoiceLine?.Name ?? "NONE";
                        vceDropDown.SelectedKeyChanged += VceDropDown_SelectedKeyChanged;

                        StackLayout vceLayout = new()
                        {
                            Orientation = Orientation.Horizontal,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Items =
                            {
                                ControlGenerator.GetControlWithLabel(parameter.Name, vceDropDown),
                                vceLink,
                            }
                        };

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(vceLayout);
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

        private void UpdatePreview()
        {
            _preview.Items.Clear();

            SKBitmap previewBitmap = new(256, 384);
            SKCanvas canvas = new(previewBitmap);
            canvas.DrawColor(SKColors.Black);

            List<ScriptItemCommand> commands = ((ScriptCommandSectionEntry)_commandsPanel.Viewer.SelectedItem).Command.WalkCommandGraph(_commands, _script.Graph);

            // Draw top screen "kinetic" background
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Verb == EventFile.CommandVerb.KBG_DISP)
                {
                    canvas.DrawBitmap(((BgScriptParameter)commands[i].Parameters[0]).Background.GetBackground(), new SKPoint(0, 0));
                    break;
                }
            }

            // Draw background
            bool bgReverted = false;
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Verb == EventFile.CommandVerb.BG_REVERT)
                {
                    bgReverted = true;
                    continue;
                }
                if (commands[i].Verb == EventFile.CommandVerb.BG_DISP || commands[i].Verb == EventFile.CommandVerb.BG_DISP2 ||
                    (commands[i].Verb == EventFile.CommandVerb.BG_FADE && (((BgScriptParameter)commands[i].Parameters[1]).Background is not null)) ||
                    (!bgReverted && (commands[i].Verb == EventFile.CommandVerb.BG_DISPTEMP || commands[i].Verb == EventFile.CommandVerb.BG_FADE)))
                {
                    BackgroundItem background = (commands[i].Verb == EventFile.CommandVerb.BG_FADE && ((BgScriptParameter)commands[i].Parameters[0]).Background is null) ?
                        ((BgScriptParameter)commands[i].Parameters[1]).Background : ((BgScriptParameter)commands[i].Parameters[0]).Background;
                    switch (background.BackgroundType)
                    {
                        case BgType.TEX_BOTTOM_TILE_TOP:
                            canvas.DrawBitmap(background.GetBackground(), new SKPoint(0, 0));
                            break;

                        default:
                            canvas.DrawBitmap(background.GetBackground(), new SKPoint(0, 194));
                            break;
                    }
                    break;
                }
            }

            // Draw top screen chibis
            List<ChibiItem> chibis = new();

            foreach (StartingChibiEntry chibi in _script.Event.StartingChibisSection?.Objects ?? new List<StartingChibiEntry>())
            {
                if (chibi.ChibiIndex > 0)
                {
                    chibis.Add((ChibiItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi && ((ChibiItem)i).ChibiIndex == chibi.ChibiIndex));
                }
            }
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].Verb == EventFile.CommandVerb.CHIBI_ENTEREXIT)
                {
                    if (((ChibiEnterExitScriptParameter)commands[i].Parameters[1]).Mode == ChibiEnterExitScriptParameter.ChibiEnterExitType.ENTER)
                    {
                        if (!chibis.Contains(((ChibiScriptParameter)commands[i].Parameters[0]).Chibi))
                        {
                            ChibiItem chibi = ((ChibiScriptParameter)commands[i].Parameters[0]).Chibi;
                            if (!chibis.Contains(chibi))
                            {
                                chibis.Add(chibi);
                            }
                            else
                            {
                                _log.LogWarning($"Chibi {chibi.Name} set to join");
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            chibis.Remove(((ChibiScriptParameter)commands[i].Parameters[0]).Chibi);
                        }
                        catch (Exception)
                        {
                            _log.LogWarning($"Chibi set to leave was not present.");
                        }
                    }
                }
            }

            int currentX, y;
            if (commands.Any(c => c.Verb == EventFile.CommandVerb.OP_MODE))
            {
                currentX = 100;
                y = 50;
            }
            else
            {
                currentX = 24;
                y = 100;
            }
            foreach (ChibiItem chibi in chibis)
            {
                SKBitmap chibiFrame = chibi.ChibiAnimations.First().Value.ElementAt(0).Frame;
                canvas.DrawBitmap(chibiFrame, new SKPoint(currentX, y));
                currentX += chibiFrame.Width - 2;
            }

            // Draw character sprites
            Dictionary<Speaker, PositionedSprite> sprites = new();

            ScriptItemCommand previousCommand = null;
            foreach (ScriptItemCommand command in commands)
            {
                if (previousCommand?.Verb == EventFile.CommandVerb.DIALOGUE)
                {
                    SpriteExitScriptParameter spriteExitMoveParam = (SpriteExitScriptParameter)previousCommand?.Parameters[3]; // exits/moves happen _after_ dialogue is advanced, so we check these at this point
                    if ((spriteExitMoveParam.ExitTransition) != SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT)
                    {
                        Speaker prevSpeaker = ((DialogueScriptParameter)previousCommand.Parameters[0]).Line.Speaker;
                        SpriteScriptParameter previousSpriteParam = (SpriteScriptParameter)previousCommand.Parameters[1];
                        short layer = ((ShortScriptParameter)previousCommand.Parameters[9]).Value;
                        switch (spriteExitMoveParam.ExitTransition)
                        {
                            case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_LEFT_FADE_OUT:
                            case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_RIGHT_FADE_OUT:
                            case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT:
                            case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT:
                            case SpriteExitScriptParameter.SpriteExitTransition.FADE_OUT_CENTER:
                            case SpriteExitScriptParameter.SpriteExitTransition.FADE_OUT_LEFT:
                                sprites.Remove(prevSpeaker);
                                break;

                            case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_CENTER_TO_LEFT_AND_STAY:
                            case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_RIGHT_TO_LEFT_AND_STAY:
                                sprites[prevSpeaker] = new() { Sprite = previousSpriteParam.Sprite, Positioning = new() { Position = SpritePositioning.SpritePosition.LEFT, Layer = layer } };
                                break;

                            case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_CENTER_TO_RIGHT_AND_STAY:
                            case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_RIGHT_AND_STAY:
                                sprites[prevSpeaker] = new() { Sprite = previousSpriteParam.Sprite, Positioning = new() { Position = SpritePositioning.SpritePosition.RIGHT, Layer = layer } };
                                break;
                        }
                    }
                }
                if (command.Verb == EventFile.CommandVerb.DIALOGUE)
                {
                    SpriteScriptParameter spriteParam = (SpriteScriptParameter)command.Parameters[1];
                    if (spriteParam.Sprite is not null)
                    {
                        Speaker speaker = ((DialogueScriptParameter)command.Parameters[0]).Line.Speaker;
                        SpriteEntranceScriptParameter spriteEntranceParam = (SpriteEntranceScriptParameter)command.Parameters[2];
                        short layer = ((ShortScriptParameter)command.Parameters[9]).Value;

                        if (!sprites.ContainsKey(speaker))
                        {
                            sprites.Add(speaker, new());
                        }
                        if (spriteEntranceParam.EntranceTransition != SpriteEntranceScriptParameter.SpriteEntranceTransition.NO_TRANSITION)
                        {
                            switch (spriteEntranceParam.EntranceTransition)
                            {
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER:
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_CENTER:
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_CENTER:
                                    sprites[speaker] = new() { Sprite = spriteParam.Sprite, Positioning = new() { Position = SpritePositioning.SpritePosition.CENTER, Layer = layer } };
                                    break;

                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_IN_LEFT:
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.PEEK_RIGHT_TO_LEFT:
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_LEFT:
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_LEFT_FAST:
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_LEFT_SLOW:
                                    sprites[speaker] = new() { Sprite = spriteParam.Sprite, Positioning = new() { Position = SpritePositioning.SpritePosition.LEFT, Layer = layer } };
                                    break;

                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_RIGHT:
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_RIGHT_FAST:
                                case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_RIGHT_SLOW:
                                    sprites[speaker] = new() { Sprite = spriteParam.Sprite, Positioning = new() { Position = SpritePositioning.SpritePosition.RIGHT, Layer = layer } };
                                    break;
                            }
                        }
                        else
                        {
                            if (sprites[speaker].Positioning is null)
                            {
                                _log.LogWarning($"Sprite {sprites[speaker]} has null positioning data!");
                            }
                            SpritePositioning.SpritePosition position = sprites[speaker].Positioning?.Position ?? SpritePositioning.SpritePosition.CENTER;

                            sprites[speaker] = new() { Sprite = spriteParam.Sprite, Positioning = new() { Position = position, Layer = layer } };
                        }
                    }
                }
                else if (command.Verb == EventFile.CommandVerb.INVEST_START)
                {
                    sprites.Clear();
                }
                previousCommand = command;
            }

            foreach (PositionedSprite sprite in sprites.Values.OrderByDescending(p => p.Positioning.Layer))
            {
                SKBitmap spriteBitmap = sprite.Sprite.GetClosedMouthAnimation(_project)[0].frame;
                canvas.DrawBitmap(spriteBitmap, sprite.Positioning.GetSpritePosition(spriteBitmap));
            }

            canvas.Flush();

            _preview.Items.Add(new SKGuiImage(previewBitmap));
        }

        private void BgSelectionButton_SelectionMade(object sender, EventArgs e)
        {
            CommandGraphicSelectionButton selection = (CommandGraphicSelectionButton)sender;
            _log.Log($"Attempting to modify parameter {selection.ParameterIndex} to background {((ItemDescription)selection.Selected).Name} in {selection.Command.Index} in file {_script.Name}...");
            ((BgScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Background =
                (BackgroundItem)_project.Items.FirstOrDefault(i => i.Name == ((ItemDescription)selection.Selected).Name);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                (short)((BackgroundItem)_project.Items.First(i => i.Name == ((ItemDescription)selection.Selected).Name)).Id;
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void BgmDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            CommandDropDown dropDown = (CommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to BGM {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((BgmScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Bgm =
                (BackgroundMusicItem)_project.Items.FirstOrDefault(i => i.Name == dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)((BackgroundMusicItem)_project.Items.First(i => i.Name == dropDown.SelectedKey)).Index;

            dropDown.Link.Text = dropDown.SelectedKey;
            dropDown.Link.RemoveAllClickEvents();
            dropDown.Link.ClickUnique += (s, e) => { _tabs.OpenTab(_project.Items.FirstOrDefault(i => i.Name == dropDown.SelectedKey), _log); };

            UpdateTabTitle(false);
        }
        private void ChibiDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            CommandDropDown dropDown = (CommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to chibi {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ChibiScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Chibi =
                (ChibiItem)_project.Items.First(i => i.Name == dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)((ChibiItem)_project.Items.First(i => i.Name == dropDown.SelectedKey)).ChibiIndex;
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ChibiEmoteDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            _log.LogWarning("Chibi emote changing not yet implemented.");
        }
        private void SpriteSelectionButton_SelectionMade(object sender, EventArgs e)
        {
            CommandGraphicSelectionButton selection = (CommandGraphicSelectionButton)sender;
            _log.Log($"Attempting to modify parameter {selection.ParameterIndex} to sprite {((ItemDescription)selection.Selected).Name} in {selection.Command.Index} in file {_script.Name}...");
            ((SpriteScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Sprite =
                (CharacterSpriteItem)selection.Selected;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                (short)((CharacterSpriteItem)selection.Selected).Index;
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void VceDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            CommandDropDown dropDown = (CommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to voiced line {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((VoicedLineScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).VoiceLine =
                (VoicedLineItem)_project.Items.FirstOrDefault(i => i.Name == dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)((VoicedLineItem)_project.Items.First(i => i.Name == dropDown.SelectedKey)).Index;

            dropDown.Link.Text = dropDown.SelectedKey;
            dropDown.Link.RemoveAllClickEvents();
            dropDown.Link.ClickUnique += (s, e) => { _tabs.OpenTab(_project.Items.FirstOrDefault(i => i.Name == dropDown.SelectedKey), _log); };

            UpdateTabTitle(false);
        }
    }
}
