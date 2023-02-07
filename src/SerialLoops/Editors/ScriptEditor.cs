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

        public ScriptEditor(ScriptItem item, Project project, ILogger log) : base(item, log, project)
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
                        CommandDropDown bgDropDown = new() { Command = command, ParameterIndex = i };
                        bgDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        bgDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background && (!bgParam.Kinetic ^ ((BackgroundItem)i).BackgroundType == BgType.KINETIC_SCREEN)).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        bgDropDown.SelectedKey = bgParam.Background?.Name ?? "NONE";
                        bgDropDown.SelectedKeyChanged += BgDropDown_SelectedKeyChanged;

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

                    case ScriptParameter.ParameterType.BGM:
                        BgmScriptParameter bgmParam = (BgmScriptParameter)parameter;
                        DropDown bgmDropDown = new();
                        bgmDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        bgmDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.BGM).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        bgmDropDown.SelectedKey = bgmParam.Bgm?.Name ?? "NONE";

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgmDropDown));
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
                        CommandDropDown spriteDropDown = new() { Command = command, ParameterIndex = i };
                        spriteDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        spriteDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Character_Sprite).Select(s => new ListItem { Text = s.Name, Key = s.Name }));
                        spriteDropDown.SelectedKey = ((SpriteScriptParameter)parameter).Sprite?.Name ?? "NONE";
                        spriteDropDown.SelectedKeyChanged += SpriteDropDown_SelectedKeyChanged;

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

            foreach (ScriptItemCommand command in commands.Where(c => c.Verb == EventFile.CommandVerb.DIALOGUE || c.Verb == EventFile.CommandVerb.LOAD_ISOMAP))
            {
                if (command.Verb == EventFile.CommandVerb.DIALOGUE)
                {
                    SpriteScriptParameter spriteParam = (SpriteScriptParameter)command.Parameters[1];
                    if (spriteParam.Sprite is not null)
                    {
                        Speaker speaker = ((DialogueScriptParameter)command.Parameters[0]).Line.Speaker;
                        SpriteEntranceScriptParameter spriteEntranceParam = (SpriteEntranceScriptParameter)command.Parameters[2];
                        SpriteExitScriptParameter spriteExitMoveParam = (SpriteExitScriptParameter)command.Parameters[3];
                        short layer = ((ShortScriptParameter)command.Parameters[9]).Value;

                        if (!sprites.ContainsKey(speaker))
                        {
                            sprites.Add(speaker, new());
                        }

                        if (spriteExitMoveParam.ExitTransition != SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT)
                        {
                            switch (spriteExitMoveParam.ExitTransition)
                            {
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_LEFT_FADE_OUT:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_RIGHT_FADE_OUT:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT:
                                case SpriteExitScriptParameter.SpriteExitTransition.FADE_OUT_CENTER:
                                case SpriteExitScriptParameter.SpriteExitTransition.FADE_OUT_LEFT:
                                    sprites.Remove(speaker);
                                    break;

                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_CENTER_TO_LEFT_AND_STAY:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_RIGHT_TO_LEFT_AND_STAY:
                                    sprites[speaker] = new() { Sprite = spriteParam.Sprite, Positioning = new() { Position = SpritePositioning.SpritePosition.LEFT, Layer = layer } };
                                    break;

                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_CENTER_TO_RIGHT_AND_STAY:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_RIGHT_AND_STAY:
                                    sprites[speaker] = new() { Sprite = spriteParam.Sprite, Positioning = new() { Position = SpritePositioning.SpritePosition.RIGHT, Layer = layer } };
                                    break;
                            }
                        }
                        else if (spriteEntranceParam.EntranceTransition != SpriteEntranceScriptParameter.SpriteEntranceTransition.NO_TRANSITION)
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
                            SpritePositioning.SpritePosition position = sprites[speaker].Positioning.Position;

                            sprites[speaker] = new() { Sprite = spriteParam.Sprite, Positioning = new() { Position = position, Layer = layer } };
                        }
                    }
                }
                else
                {
                    sprites.Clear();
                }
            }

            foreach (PositionedSprite sprite in sprites.Values.OrderBy(p => p.Positioning.Layer))
            {
                SKBitmap spriteBitmap = sprite.Sprite.GetClosedMouthAnimation(_project)[0].frame;
                canvas.DrawBitmap(spriteBitmap, sprite.Positioning.GetSpritePosition(spriteBitmap));
            }

            canvas.Flush();

            _preview.Items.Add(new SKGuiImage(previewBitmap));
        }

        private void BgDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            CommandDropDown dropDown = (CommandDropDown)sender;
            ((BgScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Background =
                (BackgroundItem)_project.Items.First(i => i.Name == dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)((BackgroundItem)_project.Items.First(i => i.Name == dropDown.SelectedKey)).Id;
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ChibiDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            CommandDropDown dropDown = (CommandDropDown)sender;
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
        private void SpriteDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            CommandDropDown dropDown = (CommandDropDown)sender;
            ((SpriteScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Sprite =
                (CharacterSpriteItem)_project.Items.First(i => i.Name == dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)((CharacterSpriteItem)_project.Items.First(i => i.Name == dropDown.SelectedKey)).Index;
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
    }
}
