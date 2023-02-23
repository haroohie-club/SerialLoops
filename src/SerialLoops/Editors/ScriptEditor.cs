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
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
        private CancellationTokenSource _dialogueCancellation, _optionCancellation;

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
            _commands = _script.GetScriptCommandTree(_project);
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

                        // BGDISPTEMP is able to display a lot more kinds of backgrounds properly than the other BG commands
                        // Hence, this switch to make sure you don't accidentally crash the game
                        switch (command.Verb)
                        {
                            case EventFile.CommandVerb.BG_DISP:
                            case EventFile.CommandVerb.BG_DISP2:
                            case EventFile.CommandVerb.BG_FADE:
                                bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background &&
                                    (((BackgroundItem)i).BackgroundType == BgType.TEX_BOTTOM || ((BackgroundItem)i).BackgroundType == BgType.TEX_BOTTOM_TEMP))
                                    .Select(b => b as IPreviewableGraphic));
                                break;

                            case EventFile.CommandVerb.BG_DISPTEMP:
                                bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background &&
                                    ((BackgroundItem)i).BackgroundType != BgType.KINETIC_SCREEN).Select(b => b as IPreviewableGraphic));
                                break;

                            case EventFile.CommandVerb.KBG_DISP:
                                bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background &&
                                    ((BackgroundItem)i).BackgroundType == BgType.KINETIC_SCREEN).Select(b => b as IPreviewableGraphic));
                                break;
                        }
                        bgSelectionButton.SelectedChanged.Executed += (obj, args) => BgSelectionButton_SelectionMade(bgSelectionButton, args);

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgSelectionButton));
                        break;

                    case ScriptParameter.ParameterType.BG_SCROLL_DIRECTION:
                        ScriptCommandDropDown bgScrollDropDown = new() { Command = command, ParameterIndex = i };
                        bgScrollDropDown.Items.AddRange(Enum.GetNames<BgScrollDirectionScriptParameter.BgScrollDirection>().Select(i => new ListItem { Text = i, Key = i }));
                        bgScrollDropDown.SelectedKey = ((BgScrollDirectionScriptParameter)parameter).ScrollDirection.ToString();
                        bgScrollDropDown.SelectedKeyChanged += BgScrollDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgScrollDropDown));
                        break;

                    case ScriptParameter.ParameterType.BGM:
                        BgmScriptParameter bgmParam = (BgmScriptParameter)parameter;
                        StackLayout bgmLink = ControlGenerator.GetFileLink(bgmParam.Bgm, _tabs, _log);

                        ScriptCommandDropDown bgmDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)bgmLink.Items[1].Control };
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
                        ScriptCommandDropDown bgmModeDropDown = new() { Command = command, ParameterIndex = i };
                        bgmModeDropDown.Items.AddRange(Enum.GetNames<BgmModeScriptParameter.BgmMode>().Select(i => new ListItem { Text = i, Key = i }));
                        bgmModeDropDown.SelectedKey = ((BgmModeScriptParameter)parameter).Mode.ToString();
                        bgmModeDropDown.SelectedKeyChanged += BgmModeDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, bgmModeDropDown));
                        break;

                    case ScriptParameter.ParameterType.BOOL:
                        ScriptCommandCheckBox boolParameterCheckbox = new() { Command = command, ParameterIndex = i, Checked = ((BoolScriptParameter)parameter).Value };
                        boolParameterCheckbox.CheckedChanged += BoolParameterCheckbox_CheckedChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, boolParameterCheckbox));
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
                        ScriptCommandDropDown chibiDropDown = new() { Command = command, ParameterIndex = i };
                        chibiDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        chibiDropDown.SelectedKey = ((ChibiScriptParameter)parameter).Chibi.Name;
                        chibiDropDown.SelectedKeyChanged += ChibiDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, chibiDropDown));
                        break;

                    case ScriptParameter.ParameterType.CHIBI_EMOTE:
                        ScriptCommandDropDown chibiEmoteDropDown = new() { Command = command, ParameterIndex = i };
                        chibiEmoteDropDown.Items.AddRange(Enum.GetNames<ChibiEmoteScriptParameter.ChibiEmote>().Select(i => new ListItem { Text = i, Key = i }));
                        chibiEmoteDropDown.SelectedKey = ((ChibiEmoteScriptParameter)parameter).Emote.ToString();
                        chibiEmoteDropDown.SelectedKeyChanged += ChibiEmoteDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, chibiEmoteDropDown));
                        break;

                    case ScriptParameter.ParameterType.CHIBI_ENTER_EXIT:
                        ScriptCommandDropDown chibiEnterExitDropDown = new() { Command = command, ParameterIndex = i };
                        chibiEnterExitDropDown.Items.AddRange(Enum.GetNames<ChibiEnterExitScriptParameter.ChibiEnterExitType>().Select(i => new ListItem { Text = i, Key = i }));
                        chibiEnterExitDropDown.SelectedKey = ((ChibiEnterExitScriptParameter)parameter).Mode.ToString();
                        chibiEnterExitDropDown.SelectedKeyChanged += ChibiEnterExitDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, chibiEnterExitDropDown));
                        break;

                    case ScriptParameter.ParameterType.COLOR:
                        ScriptCommandColorPicker colorPicker = new()
                        {
                            AllowAlpha = false,
                            Value = ((ColorScriptParameter)parameter).Color.ToEtoDrawingColor(),
                            Command = command,
                            ParameterIndex = i,
                        };
                        colorPicker.ValueChanged += ColorPicker_ValueChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, colorPicker));
                        break;

                    case ScriptParameter.ParameterType.CONDITIONAL:
                        ScriptCommandTextBox conditionalBox = new() { Text = ((ConditionalScriptParameter)parameter).Value, Command = command, ParameterIndex = i };
                        conditionalBox.TextChanged += ConditionalBox_TextChanged;
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            conditionalBox));
                        break;

                    case ScriptParameter.ParameterType.COLOR_MONOCHROME:
                        ScriptCommandDropDown colorMonochromeDropDown = new() { Command = command, ParameterIndex = i };
                        colorMonochromeDropDown.Items.AddRange(Enum.GetNames<ColorMonochromeScriptParameter.ColorMonochrome>().Select(i => new ListItem { Text = i, Key = i }));
                        colorMonochromeDropDown.SelectedKey = ((ColorMonochromeScriptParameter)parameter).ColorType.ToString();
                        colorMonochromeDropDown.SelectedKeyChanged += ColorMonochromeDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, colorMonochromeDropDown));
                        break;

                    case ScriptParameter.ParameterType.DIALOGUE:
                        DialogueScriptParameter dialogueParam = (DialogueScriptParameter)parameter;
                        ScriptCommandDropDown speakerDropDown = new() { Command = command, ParameterIndex = i };
                        speakerDropDown.Items.AddRange(Enum.GetNames<Speaker>().Select(s => new ListItem { Text = s, Key = s }));
                        speakerDropDown.SelectedKey = dialogueParam.Line.Speaker.ToString();
                        speakerDropDown.SelectedKeyChanged += SpeakerDropDown_SelectedKeyChanged;
                        if (currentCol > 0)
                        {
                            controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                            currentRow++;
                            currentCol = 0;
                        }

                        ScriptCommandTextArea dialogueTextArea = new() 
                        {
                            Text = dialogueParam.Line.Text.GetSubstitutedString(_project),
                            AcceptsReturn = true,
                            Command = command,
                            ParameterIndex = i,
                        };
                        dialogueTextArea.TextChanged += DialogueTextArea_TextChanged;
                        dialogueTextArea.LostFocus += DialogueTextArea_LostFocus;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabelTable(parameter.Name,
                            new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Items =
                                {
                                    speakerDropDown,
                                    new StackLayoutItem(dialogueTextArea, expand: true),
                                },
                            }));
                        controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows.Add(new());
                        currentRow++;
                        currentCol = 0;
                        break;

                    case ScriptParameter.ParameterType.DIALOGUE_PROPERTY:
                        ScriptCommandDropDown dialoguePropertyDropDown = new() { Command = command, ParameterIndex = i };
                        dialoguePropertyDropDown.Items.AddRange(Enum.GetNames<Speaker>().Select(t => new ListItem { Text = t, Key = t }));
                        dialoguePropertyDropDown.SelectedKey = ((DialoguePropertyScriptParameter)parameter).DialogueProperties.Character.ToString();
                        dialoguePropertyDropDown.SelectedKeyChanged += DialoguePropertyDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, dialoguePropertyDropDown));
                        break;

                    case ScriptParameter.ParameterType.EPISODE_HEADER:
                        ScriptCommandDropDown epHeaderDropDown = new() { Command = command, ParameterIndex = i };
                        epHeaderDropDown.Items.AddRange(Enum.GetNames<EpisodeHeaderScriptParameter.Episode>().Select(e => new ListItem { Key = e, Text = e }));
                        epHeaderDropDown.SelectedKey = ((EpisodeHeaderScriptParameter)parameter).EpisodeHeaderIndex.ToString();
                        epHeaderDropDown.SelectedKeyChanged += EpHeaderDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            epHeaderDropDown));
                        break;

                    case ScriptParameter.ParameterType.FLAG:
                        ScriptCommandTextBox flagTextBox = new() { Command = command, ParameterIndex = i, Text = ((FlagScriptParameter)parameter).FlagName };
                        flagTextBox.TextChanged += FlagTextBox_TextChanged;
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, flagTextBox));
                        break;

                    case ScriptParameter.ParameterType.ITEM:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((ItemScriptParameter)parameter).ItemIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.MAP:
                        MapScriptParameter mapParam = (MapScriptParameter)parameter;
                        ScriptCommandDropDown mapDropDown = new() { Command = command, ParameterIndex = i };
                        mapDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        mapDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Map).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                        mapDropDown.SelectedKey = mapParam.Map?.Name ?? "NONE";
                        mapDropDown.SelectedKeyChanged += MapDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, mapDropDown));
                        break;

                    case ScriptParameter.ParameterType.OPTION:
                        OptionScriptParameter optionParam = (OptionScriptParameter)parameter;
                        ScriptCommandDropDown optionScriptSectionDropDown = new() { Command = command, ParameterIndex = i };
                        optionScriptSectionDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                        optionScriptSectionDropDown.Items.AddRange(_script.Event.ScriptSections.Skip(1).Select(s => new ListItem { Text = s.Name, Key = s.Name }));
                        optionScriptSectionDropDown.SelectedKey = optionParam.Option.Id == 0 ? "NONE" : _script.Event.LabelsSection.Objects.First(l => l.Id == optionParam.Option.Id).Name.Replace("/", "");
                        optionScriptSectionDropDown.SelectedKeyChanged += OptionScriptSectionDropDown_SelectedKeyChanged;

                        ScriptCommandTextBox optionTextBox = new() { Command = command, ParameterIndex = i, Text = optionParam.Option.Text.GetSubstitutedString(_project) };
                        optionTextBox.TextChanged += OptionTextBox_TextChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Items =
                                {
                                    optionScriptSectionDropDown,
                                    new StackLayoutItem(optionTextBox, expand: true),
                                }
                            }));
                        break;

                    case ScriptParameter.ParameterType.PALETTE_EFFECT:
                        ScriptCommandDropDown paletteEffectDropDown = new() { Command = command, ParameterIndex = i };
                        paletteEffectDropDown.Items.AddRange(Enum.GetNames<PaletteEffectScriptParameter.PaletteEffect>().Select(t => new ListItem { Text = t, Key = t }));
                        paletteEffectDropDown.SelectedKey = ((PaletteEffectScriptParameter)parameter).Effect.ToString();
                        paletteEffectDropDown.SelectedKeyChanged += PaletteEffectDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, paletteEffectDropDown));
                        break;

                    case ScriptParameter.ParameterType.PLACE:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((PlaceScriptParameter)parameter).PlaceIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.SCREEN:
                        ScriptCommandDropDown screenDropDown = new() { Command = command, ParameterIndex = i };
                        screenDropDown.Items.AddRange(Enum.GetNames<ScreenScriptParameter.DsScreen>().Select(t => new ListItem { Text = t, Key = t }));
                        screenDropDown.SelectedKey = ((ScreenScriptParameter)parameter).Screen.ToString();
                        screenDropDown.SelectedKeyChanged += ScreenDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, screenDropDown));
                        break;

                    case ScriptParameter.ParameterType.SCRIPT_SECTION:
                        ScriptCommandDropDown scriptSectionDropDown = new() { Command = command, ParameterIndex = i };
                        scriptSectionDropDown.Items.AddRange(_script.Event.ScriptSections.Select(s => new ListItem { Text = s.Name, Key = s.Name }));
                        scriptSectionDropDown.SelectedKey = ((ScriptSectionScriptParameter)parameter)?.Section?.Name ?? "NONE";
                        scriptSectionDropDown.SelectedKeyChanged += ScriptSectionDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, scriptSectionDropDown));
                        break;

                    case ScriptParameter.ParameterType.SFX:
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name,
                            new TextBox { Text = ((SfxScriptParameter)parameter).SfxIndex.ToString() }));
                        break;

                    case ScriptParameter.ParameterType.SFX_MODE:
                        ScriptCommandDropDown sfxModeDropDown = new() { Command = command, ParameterIndex = i };
                        sfxModeDropDown.Items.AddRange(Enum.GetNames<SfxModeScriptParameter.SfxMode>().Select(t => new ListItem { Text = t, Key = t }));
                        sfxModeDropDown.SelectedKey = ((SfxModeScriptParameter)parameter).Mode.ToString();
                        sfxModeDropDown.SelectedKeyChanged += SfxModeDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, sfxModeDropDown));
                        break;

                    case ScriptParameter.ParameterType.SHORT:
                        ScriptCommandTextBox shortTextBox = new() { Command = command, ParameterIndex = i, Text = ((ShortScriptParameter)parameter).Value.ToString() };
                        shortTextBox.TextChanged += ShortTextBox_TextChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, shortTextBox));
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
                        ScriptCommandDropDown spriteEntranceDropDown = new() { Command = command, ParameterIndex = i };
                        spriteEntranceDropDown.Items.AddRange(Enum.GetNames<SpriteEntranceScriptParameter.SpriteEntranceTransition>().Select(t => new ListItem { Text = t, Key = t }));
                        spriteEntranceDropDown.SelectedKey = ((SpriteEntranceScriptParameter)parameter).EntranceTransition.ToString();
                        spriteEntranceDropDown.SelectedKeyChanged += SpriteEntranceDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, spriteEntranceDropDown));
                        break;

                    case ScriptParameter.ParameterType.SPRITE_EXIT:
                        ScriptCommandDropDown spriteExitDropDown = new() { Command = command, ParameterIndex = i };
                        spriteExitDropDown.Items.AddRange(Enum.GetNames<SpriteExitScriptParameter.SpriteExitTransition>().Select(t => new ListItem { Text = t, Key = t }));
                        spriteExitDropDown.SelectedKey = ((SpriteExitScriptParameter)parameter).ExitTransition.ToString();
                        spriteExitDropDown.SelectedKeyChanged += SpriteExitDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, spriteExitDropDown));
                        break;

                    case ScriptParameter.ParameterType.SPRITE_SHAKE:
                        ScriptCommandDropDown spriteShakeDropDown = new() { Command = command, ParameterIndex = i };
                        spriteShakeDropDown.Items.AddRange(Enum.GetNames<SpriteShakeScriptParameter.SpriteShakeEffect>().Select(t => new ListItem { Text = t, Key = t }));
                        spriteShakeDropDown.SelectedKey = ((SpriteShakeScriptParameter)parameter).ShakeEffect.ToString();
                        spriteShakeDropDown.SelectedKeyChanged += SpriteShakeDropDown_SelectedKeyChanged;

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, spriteShakeDropDown));
                        break;

                    case ScriptParameter.ParameterType.TEXT_ENTRANCE_EFFECT:
                        ScriptCommandDropDown textEntranceEffectDropDown = new() { Command = command, ParameterIndex = i };
                        textEntranceEffectDropDown.Items.AddRange(Enum.GetNames<TextEntranceEffectScriptParameter.TextEntranceEffect>().Select(t => new ListItem { Text = t, Key = t }));
                        textEntranceEffectDropDown.SelectedKey = ((TextEntranceEffectScriptParameter)parameter).EntranceEffect.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, textEntranceEffectDropDown));
                        break;

                    case ScriptParameter.ParameterType.TOPIC:
                        string topicName = _project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic &&
                            ((TopicItem)i).Topic.Id == ((TopicScriptParameter)parameter).TopicId)?.DisplayName;
                        if (string.IsNullOrEmpty(topicName))
                        {
                            // If the topic has been deleted, we will just display the index in a textbox
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name,
                                new TextBox { Text = ((TopicScriptParameter)parameter).TopicId.ToString() }));
                        }
                        else
                        {
                            StackLayout topicLink = ControlGenerator.GetFileLink(_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic &&
                                ((TopicItem)i).Topic.Id == ((TopicScriptParameter)parameter).TopicId), _tabs, _log);

                            ScriptCommandDropDown topicDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)topicLink.Items[1].Control };
                            topicDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Topic)
                                .Select(t => new ListItem { Key = t.DisplayName, Text = t.DisplayName }));
                            topicDropDown.SelectedKey = topicName;
                            topicDropDown.SelectedIndexChanged += TopicDropDown_SelectedIndexChanged;

                            StackLayout topicLinkLayout = new()
                            {
                                Orientation = Orientation.Horizontal,
                                Items =
                                {
                                    topicDropDown,
                                    topicLink,
                                },
                            };

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name,
                                topicLinkLayout));
                        }
                        break;

                    case ScriptParameter.ParameterType.TRANSITION:
                        DropDown transitionDropDown = new();
                        transitionDropDown.Items.AddRange(Enum.GetNames<TransitionScriptParameter.TransitionEffect>().Select(t => new ListItem { Text = t, Key = t }));
                        transitionDropDown.SelectedKey = ((TransitionScriptParameter)parameter).Transition.ToString();

                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                            ControlGenerator.GetControlWithLabel(parameter.Name, transitionDropDown));
                        break;

                    case ScriptParameter.ParameterType.VOICE_LINE:
                        VoicedLineScriptParameter vceParam = (VoicedLineScriptParameter)parameter;
                        StackLayout vceLink = ControlGenerator.GetFileLink(vceParam.VoiceLine is not null ? vceParam.VoiceLine : NoneItem.VOICE, _tabs, _log);

                        ScriptCommandDropDown vceDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)vceLink.Items[1].Control };
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

            ScriptItemCommand currentCommand = ((ScriptCommandSectionEntry)_commandsPanel.Viewer.SelectedItem).Command;
            List<ScriptItemCommand> commands = currentCommand.WalkCommandGraph(_commands, _script.Graph);

            if (commands is null)
            {
                _log.LogError($"Unable to render preview for command as commands list was null.");
                return;
            }

            // Draw top screen "kinetic" background
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Verb == EventFile.CommandVerb.KBG_DISP)
                {
                    canvas.DrawBitmap(((BgScriptParameter)commands[i].Parameters[0]).Background.GetBackground(), new SKPoint(0, 0));
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

            int chibiStartX, chibiY;
            if (commands.Any(c => c.Verb == EventFile.CommandVerb.OP_MODE))
            {
                chibiStartX = 100;
                chibiY = 50;
            }
            else
            {
                chibiStartX = 24;
                chibiY = 100;
            }
            int chibiCurrentX = chibiStartX;
            int chibiWidth = 0;
            foreach (ChibiItem chibi in chibis)
            {
                SKBitmap chibiFrame = chibi.ChibiAnimations.First().Value.ElementAt(0).Frame;
                canvas.DrawBitmap(chibiFrame, new SKPoint(chibiCurrentX, chibiY));
                chibiWidth = chibiFrame.Width - 2;
                chibiCurrentX += chibiWidth;
            }

            // Draw top screen chibi emotes
            if (currentCommand.Verb == EventFile.CommandVerb.CHIBI_EMOTE)
            {
                ChibiItem chibi = ((ChibiScriptParameter)currentCommand.Parameters[0]).Chibi;
                if (chibis.Contains(chibi))
                {
                    int chibiIndex = chibis.IndexOf(chibi);
                    SKBitmap emotes = _project.Grp.Files.First(f => f.Name == "SYS_ADV_T08DNX").GetImage(width: 32, transparentIndex: 0);
                    int internalYOffset = ((int)((ChibiEmoteScriptParameter)currentCommand.Parameters[1]).Emote - 1) * 32;
                    int externalXOffset = chibiStartX + chibiWidth * chibiIndex;
                    canvas.DrawBitmap(emotes, new SKRect(0, internalYOffset, 32, internalYOffset + 32), new SKRect(externalXOffset + 16, chibiY - 32, externalXOffset + 48, chibiY));
                }
                else
                {
                    _log.LogWarning($"Chibi {chibi.Name} not currently on screen; cannot display emote.");
                }
            }

            // Draw background
            bool bgReverted = false;
            ScriptItemCommand bgPalCommand = commands.LastOrDefault(c => c.Verb == EventFile.CommandVerb.BG_PALEFFECT);
            ScriptItemCommand lastBgCommand = commands.LastOrDefault(c => c.Verb == EventFile.CommandVerb.BG_DISP ||
                c.Verb == EventFile.CommandVerb.BG_DISP2 || c.Verb == EventFile.CommandVerb.BG_DISPTEMP || c.Verb == EventFile.CommandVerb.BG_FADE ||
                c.Verb == EventFile.CommandVerb.BG_REVERT);
            SKPaint bgEffectPaint = PaletteEffectScriptParameter.IdentityPaint;
            if (bgPalCommand is not null && lastBgCommand is not null && commands.IndexOf(bgPalCommand) > commands.IndexOf(lastBgCommand))
            {
                switch (((PaletteEffectScriptParameter)bgPalCommand.Parameters[0]).Effect)
                {
                    case PaletteEffectScriptParameter.PaletteEffect.INVERTED:
                        bgEffectPaint = PaletteEffectScriptParameter.InvertedPaint;
                        break;

                    case PaletteEffectScriptParameter.PaletteEffect.GRAYSCALE:
                        bgEffectPaint = PaletteEffectScriptParameter.GrayscalePaint;
                        break;

                    case PaletteEffectScriptParameter.PaletteEffect.SEPIA:
                        bgEffectPaint = PaletteEffectScriptParameter.SepiaPaint;
                        break;

                    case PaletteEffectScriptParameter.PaletteEffect.DIMMED:
                        bgEffectPaint = PaletteEffectScriptParameter.DimmedPaint;
                        break;
                }
            }
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
                    if (background is not null)
                    {
                        switch (background.BackgroundType)
                        {
                            case BgType.TEX_DUAL:
                                canvas.DrawBitmap(background.GetBackground(), new SKPoint(0, 0), bgEffectPaint);
                                break;

                            case BgType.SINGLE_TEX:
                                if (commands[i].Verb == EventFile.CommandVerb.BG_DISPTEMP && ((BoolScriptParameter)commands[i].Parameters[1]).Value)
                                {
                                    SKBitmap bgBitmap = background.GetBackground();
                                    canvas.DrawBitmap(bgBitmap, new SKRect(0, bgBitmap.Height - 194, bgBitmap.Width, bgBitmap.Height), 
                                        new SKRect(0, 194, 256, 388), bgEffectPaint);
                                }
                                else
                                {
                                    canvas.DrawBitmap(background.GetBackground(), new SKPoint(0, 194), bgEffectPaint);
                                }
                                break;

                            default:
                                canvas.DrawBitmap(background.GetBackground(), new SKPoint(0, 194), bgEffectPaint);
                                break;
                        }
                        break;
                    }
                }
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
            if (((ItemDescription)selection.Selected).Name == "NONE")
            {
                ((BgScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Background =
                    (BackgroundItem)_project.Items.FirstOrDefault(i => i.Name == ((ItemDescription)selection.Selected).Name);
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] = 0;
            }
            else
            {
                ((BgScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Background =
                    (BackgroundItem)_project.Items.FirstOrDefault(i => i.Name == ((ItemDescription)selection.Selected).Name);
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                    (short)((BackgroundItem)_project.Items.First(i => i.Name == ((ItemDescription)selection.Selected).Name)).Id;
            }
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void BgScrollDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to BG scroll direction {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((BgScrollDirectionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).ScrollDirection =
                Enum.Parse<BgScrollDirectionScriptParameter.BgScrollDirection>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<BgScrollDirectionScriptParameter.BgScrollDirection>(dropDown.SelectedKey);

            UpdateTabTitle(false);
        }
        private void BgmDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
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
        private void BgmModeDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to BGM mode {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((BgmModeScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Mode =
                Enum.Parse<BgmModeScriptParameter.BgmMode>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<BgmModeScriptParameter.BgmMode>(dropDown.SelectedKey);

            UpdateTabTitle(false);
        }
        private void BoolParameterCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            ScriptCommandCheckBox checkBox = (ScriptCommandCheckBox)sender;
            _log.Log($"Attempting to modify parameter {checkBox.ParameterIndex} to BGM mode {checkBox.Checked} in {checkBox.Command.Index} in file {_script.Name}...");
            ((BoolScriptParameter)checkBox.Command.Parameters[checkBox.ParameterIndex]).Value = checkBox.Checked ?? false;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(checkBox.Command.Section)]
                .Objects[checkBox.Command.Index].Parameters[checkBox.ParameterIndex] = (short)((checkBox.Checked ?? false) ? 1 : 0);

            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ChibiDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
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
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to chibi emote {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ChibiEmoteScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Emote =
                Enum.Parse<ChibiEmoteScriptParameter.ChibiEmote>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<ChibiEmoteScriptParameter.ChibiEmote>(dropDown.SelectedKey);
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ChibiEnterExitDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to chibi enter/exit {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ChibiEnterExitScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Mode =
                Enum.Parse<ChibiEnterExitScriptParameter.ChibiEnterExitType>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<ChibiEnterExitScriptParameter.ChibiEnterExitType>(dropDown.SelectedKey);
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ColorPicker_ValueChanged(object sender, EventArgs e)
        {
            ScriptCommandColorPicker colorPicker = (ScriptCommandColorPicker)sender;
            _log.Log($"Attempting to modify parameters {colorPicker.ParameterIndex} through {colorPicker.ParameterIndex + 2} to color #{colorPicker.Value.ToHex()} in {colorPicker.Command.Index} in file {_script.Name}...");
            SKColor color = colorPicker.Value.ToSKColor();
            ((ColorScriptParameter)colorPicker.Command.Parameters[colorPicker.ParameterIndex]).Color = color;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(colorPicker.Command.Section)]
                .Objects[colorPicker.Command.Index].Parameters[colorPicker.ParameterIndex] = (short)color.Red;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(colorPicker.Command.Section)]
                .Objects[colorPicker.Command.Index].Parameters[colorPicker.ParameterIndex + 1] = (short)color.Green;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(colorPicker.Command.Section)]
                .Objects[colorPicker.Command.Index].Parameters[colorPicker.ParameterIndex + 2] = (short)color.Blue;
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ColorMonochromeDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to monochrome color {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ColorMonochromeScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).ColorType =
                Enum.Parse<ColorMonochromeScriptParameter.ColorMonochrome>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<ColorMonochromeScriptParameter.ColorMonochrome>(dropDown.SelectedKey);
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ConditionalBox_TextChanged(object sender, EventArgs e)
        {
            ScriptCommandTextBox textBox = (ScriptCommandTextBox)sender;
            _log.Log($"Attempting to modify parameter {textBox.ParameterIndex} to conditional {textBox.Text} in {textBox.Command.Index} in file {_script.Name}...");
            ((ConditionalScriptParameter)textBox.Command.Parameters[textBox.ParameterIndex]).Value = textBox.Text;
            if (_script.Event.ConditionalsSection.Objects.Contains(textBox.Text))
            {
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(textBox.Command.Section)]
                    .Objects[textBox.Command.Index].Parameters[textBox.ParameterIndex] = (short)_script.Event.ConditionalsSection.Objects.IndexOf(textBox.Text);
            }
            else
            {
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(textBox.Command.Section)]
                    .Objects[textBox.Command.Index].Parameters[textBox.ParameterIndex] = (short)_script.Event.ConditionalsSection.Objects.Count;
                _script.Event.ConditionalsSection.Objects.Add(textBox.Text);
            }
            UpdateTabTitle(false);
        }
        private void SpeakerDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify speaker in parameter {dropDown.ParameterIndex} to speaker {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((DialogueScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Line.Speaker =
                Enum.Parse<Speaker>(dropDown.SelectedKey);
            _script.Event.DialogueSection.Objects[dropDown.Command.Section.Objects[dropDown.Command.Index].Parameters[0]].Speaker =
                Enum.Parse<Speaker>(dropDown.SelectedKey);
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void DialogueTextArea_TextChanged(object sender, EventArgs e)
        {
            ScriptCommandTextArea textArea = (ScriptCommandTextArea)sender;

            textArea.Text = Regex.Replace(textArea.Text, @"^""", "“");
            textArea.Text = Regex.Replace(textArea.Text, @"""\s", "“");
            textArea.Text = textArea.Text.Replace('"', '”');

            _log.Log($"Attempting to modify dialogue in parameter {textArea.ParameterIndex} to dialogue '{textArea.Text}' in {textArea.Command.Index} in file {_script.Name}...");

            _dialogueCancellation?.Cancel();
            _dialogueCancellation = new();

            string text = textArea.Text;
            ScriptItemCommand command = textArea.Command;
            int parameterIndex = textArea.ParameterIndex;
            Task task = new(() =>
            {
                string originalText = text.GetOriginalString(_project);
                ((DialogueScriptParameter)command.Parameters[parameterIndex]).Line.Text = originalText;
                _script.Event.DialogueSection.Objects[command.Section.Objects[command.Index].Parameters[0]].Text = originalText;
                _dialogueCancellation = null;
            }, _dialogueCancellation.Token);
            task.Start();
            UpdateTabTitle(false);
        }
        private void DialogueTextArea_LostFocus(object sender, EventArgs e)
        {
            UpdatePreview();
        }
        private void DialoguePropertyDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            MessageInfoFile messInfoFile = _project.Dat.Files.First(d => d.Name == "MESSINFOS").CastTo<MessageInfoFile>();
            MessageInfo messInfo = messInfoFile.MessageInfos.First(m => m.Character == Enum.Parse<Speaker>(dropDown.SelectedKey));
            _log.Log($"Attempting to modify dialogue property in parameter {dropDown.ParameterIndex} to dialogue {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((DialoguePropertyScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).DialogueProperties = messInfo;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)].Objects[dropDown.Command.Index]
                .Parameters[dropDown.ParameterIndex] = (short)messInfoFile.MessageInfos.IndexOf(messInfo);
            UpdateTabTitle(false);
        }
        private void EpHeaderDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify episode header in parameter {dropDown.ParameterIndex} to speaker {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((EpisodeHeaderScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).EpisodeHeaderIndex =
                Enum.Parse<EpisodeHeaderScriptParameter.Episode>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<EpisodeHeaderScriptParameter.Episode>(dropDown.SelectedKey);
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void FlagTextBox_TextChanged(object sender, EventArgs e)
        {
            ScriptCommandTextBox textBox = (ScriptCommandTextBox)sender;
            if ((textBox.Text.StartsWith("F", StringComparison.OrdinalIgnoreCase) || textBox.Text.StartsWith("G", StringComparison.OrdinalIgnoreCase)) && short.TryParse(textBox.Text[1..], out short flagId))
            {
                _log.Log($"Attempting to modify parameter {textBox.ParameterIndex} to flag {textBox.Text} in {textBox.Command.Index} in file {_script.Name}...");
                ((FlagScriptParameter)textBox.Command.Parameters[textBox.ParameterIndex]).Id = flagId;
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(textBox.Command.Section)]
                    .Objects[textBox.Command.Index].Parameters[textBox.ParameterIndex] = ((FlagScriptParameter)textBox.Command.Parameters[textBox.ParameterIndex]).Id;
                UpdateTabTitle(false);
            }
        }
        private void MapDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to map {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((MapScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Map = (MapItem)_project.FindItem(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] = (short)((MapScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Map.Map.Index;
            UpdateTabTitle(false);
        }
        private void OptionScriptSectionDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify script section in parameter {dropDown.ParameterIndex} to script section {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            short newScriptSectionId = _script.Event.LabelsSection.Objects.First(l => ((OptionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Option.Text.Replace("/", "") == dropDown.SelectedKey).Id;
            ((OptionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Option.Id = newScriptSectionId;
            _script.Event.ChoicesSection.Objects[dropDown.ParameterIndex].Id = newScriptSectionId;
            UpdateTabTitle(false);
        }
        private void OptionTextBox_TextChanged(object sender, EventArgs e)
        {
            ScriptCommandTextBox textBox = (ScriptCommandTextBox)sender;

            textBox.Text = Regex.Replace(textBox.Text, @"^""", "“");
            textBox.Text = Regex.Replace(textBox.Text, @"""\s", "“");
            textBox.Text = textBox.Text.Replace('"', '”');

            _log.Log($"Attempting to modify option text in parameter {textBox.ParameterIndex} to text '{textBox.Text}' in '{textBox.Command.Index}' in file {_script.Name}...");

            _optionCancellation?.Cancel();
            _optionCancellation = new();

            string text = textBox.Text;
            ScriptItemCommand command = textBox.Command;
            int parameterIndex = textBox.ParameterIndex;
            Task task = new(() =>
            {
                string originalText = text.GetOriginalString(_project);
                ((OptionScriptParameter)command.Parameters[parameterIndex]).Option.Text = originalText;
                _script.Event.ChoicesSection.Objects[command.Section.Objects[command.Index].Parameters[0]].Text = originalText;
                _optionCancellation = null;
            }, _optionCancellation.Token);
            task.Start();
            UpdateTabTitle(false);
        }
        private void PaletteEffectDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to BG palette effect {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((PaletteEffectScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Effect =
                Enum.Parse<PaletteEffectScriptParameter.PaletteEffect>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<PaletteEffectScriptParameter.PaletteEffect>(dropDown.SelectedKey);
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ScreenDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to screen {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ScreenScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Screen =
                Enum.Parse<ScreenScriptParameter.DsScreen>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<ScreenScriptParameter.DsScreen>(dropDown.SelectedKey);
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ScriptSectionDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to script section {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ScriptSectionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Section = _script.Event.ScriptSections.First(s => s.Name.Replace("/", "") == dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                _script.Event.LabelsSection.Objects.First(l => ((ScriptSectionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Section.Name.Replace("/", "") == dropDown.SelectedKey).Id;
            UpdateTabTitle(false);
        }
        private void SfxModeDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to SFX mode {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((SfxModeScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Mode =
                Enum.Parse<SfxModeScriptParameter.SfxMode>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<SfxModeScriptParameter.SfxMode>(dropDown.SelectedKey);
            UpdateTabTitle(false);
        }
        private void ShortTextBox_TextChanged(object sender, EventArgs e)
        {
            ScriptCommandTextBox textBox = (ScriptCommandTextBox)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to SFX mode {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
        }
        private void SpriteEntranceDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to sprite entrance {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((SpriteEntranceScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).EntranceTransition =
                Enum.Parse<SpriteEntranceScriptParameter.SpriteEntranceTransition>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<SpriteEntranceScriptParameter.SpriteEntranceTransition>(dropDown.SelectedKey);
            UpdateTabTitle(false);
        }
        private void SpriteExitDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to sprite exit {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((SpriteExitScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).ExitTransition =
                Enum.Parse<SpriteExitScriptParameter.SpriteExitTransition>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<SpriteExitScriptParameter.SpriteExitTransition>(dropDown.SelectedKey);
            UpdateTabTitle(false);
        }
        private void SpriteShakeDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to sprite shake {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((SpriteShakeScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).ShakeEffect =
                Enum.Parse<SpriteShakeScriptParameter.SpriteShakeEffect>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<SpriteShakeScriptParameter.SpriteShakeEffect>(dropDown.SelectedKey);
            UpdateTabTitle(false);
        }
        private void SpriteSelectionButton_SelectionMade(object sender, EventArgs e)
        {
            CommandGraphicSelectionButton selection = (CommandGraphicSelectionButton)sender;
            _log.Log($"Attempting to modify parameter {selection.ParameterIndex} to sprite {((ItemDescription)selection.Selected).Name} in {selection.Command.Index} in file {_script.Name}...");
            if (((ItemDescription)selection.Selected).Name == "NONE")
            {
                ((SpriteScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Sprite =
                    null;
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                    0;
            }
            else
            {
                ((SpriteScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Sprite =
                    (CharacterSpriteItem)selection.Selected;
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                    (short)((CharacterSpriteItem)selection.Selected).Index;
            }
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void VceDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to voiced line {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            if (dropDown.SelectedKey == "NONE")
            {
                ((VoicedLineScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).VoiceLine = null;
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                    .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] = 0;
            }
            else
            {
                ((VoicedLineScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).VoiceLine =
                    (VoicedLineItem)_project.Items.FirstOrDefault(i => i.Name == dropDown.SelectedKey);
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                    .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                    (short)((VoicedLineItem)_project.Items.First(i => i.Name == dropDown.SelectedKey)).Index;
            }
            dropDown.Link.Text = dropDown.SelectedKey;
            dropDown.Link.RemoveAllClickEvents();
            dropDown.Link.ClickUnique += (s, e) => { _tabs.OpenTab(_project.Items.FirstOrDefault(i => i.Name == dropDown.SelectedKey), _log); };

            UpdateTabTitle(false);
        }
        private void TopicDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to topic {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((TopicScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).TopicId =
                ((TopicItem)_project.Items.FirstOrDefault(i => i.Name == dropDown.SelectedKey)).Topic.Id;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                ((TopicItem)_project.Items.First(i => i.Name == dropDown.SelectedKey)).Topic.Id;

            dropDown.Link.Text = dropDown.SelectedKey;
            dropDown.Link.RemoveAllClickEvents();
            dropDown.Link.ClickUnique += (s, e) => { _tabs.OpenTab(_project.Items.FirstOrDefault(i => i.Name == dropDown.SelectedKey), _log); };

            UpdateTabTitle(false);
        }
    }
}
