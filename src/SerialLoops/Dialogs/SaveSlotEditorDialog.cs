using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Save;
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
using System.IO;
using System.Linq;
using System.Reflection;

namespace SerialLoops.Dialogs
{
    public class SaveSlotEditorDialog : FloatingForm
    {
        private readonly ILogger _log;
        private readonly SaveSection _saveSection;
        private readonly string _fileName;
        private readonly string _slotName;
        private readonly Project _project;
        private readonly EditorTabsPanel _tabs;
        private readonly Action _callback;

        private readonly Dictionary<int, bool> _flagModifications = [];
        private readonly List<Control> _modifierControls = [];
        private ScriptItem _quickSaveScript;
        private ScriptPreview _quickSaveScriptPreview;

        private const int FLAGS_PER_PAGE = 12;
        private readonly StackLayout _checkBoxContainer = new();
        private int _flagPage = 1;
        private List<int> _flags = [];

        public SaveSlotEditorDialog(ILogger log, SaveSection saveSection, string fileName, string slotName,
                                    Project project, EditorTabsPanel tabs, Action callback)
        {
            _log = log;
            _saveSection = saveSection;
            _fileName = fileName;
            _slotName = slotName;
            _project = project;
            _tabs = tabs;
            _callback = callback;
            _flags = GetFilteredFlags();

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = $"Edit Save File - {_fileName} - {_slotName}"; // already loc'd these on input, so this is fine
            Width = 550;
            Height = 560;
            Padding = 10;

            TabControl menuTabs = new();
            TableLayout layout = new()
            {
                Spacing = new(5, 10),
                Height = 500,
                Rows =
                {
                    ControlGenerator.GetTextHeader($"{_slotName}"),
                    menuTabs,
                }
            };

            if (_saveSection is CommonSaveData commonSave)
            {
                menuTabs.Pages.Add(new TabPage { Content = GetCommonDataBox(commonSave), Text = Application.Instance.Localize(this, "Config Data") });
            }
            else
            {
                SaveSlotData slotSave = (SaveSlotData)_saveSection;
                menuTabs.Pages.Add(new TabPage { Content = GetSlotDataBox(slotSave), Text = Application.Instance.Localize(this, "Save Data") });

                if (slotSave is QuickSaveSlotData quickSave)
                {
                    menuTabs.Pages.Add(new TabPage { Content = GetQuickSaveDataBox(quickSave), Text = Application.Instance.Localize(this, "Script Position") });
                }
            }
            TabPage flagsTab = new() { Content = GetFlagsBox(), Text = Application.Instance.Localize(this, "Flags") };
            flagsTab.Shown += (sender, args) => UpdateFlagList(_flagPage);
            menuTabs.Pages.Add(flagsTab);


            Button saveButton = new() { Text = Application.Instance.Localize(this, "Save") };
            saveButton.Click += (sender, args) =>
            {
                foreach (int flag in _flagModifications.Keys)
                {
                    if (_flagModifications[flag])
                    {
                        _saveSection.SetFlag(flag);
                    }
                    else
                    {
                        _saveSection.ClearFlag(flag);
                    }
                }

                if (_saveSection is CommonSaveData commonSave)
                {
                    commonSave.NumSaves = (int)((NumericStepper)_modifierControls.First(c => c.ID == "NumSaves")).Value;
                    commonSave.Options.BgmVolume = (int)((NumericStepper)_modifierControls.First(c => c.ID == "BgmVolume")).Value * 10;
                    commonSave.Options.SfxVolume = (int)((NumericStepper)_modifierControls.First(c => c.ID == "SfxVolume")).Value * 10;
                    commonSave.Options.WordsVolume = (int)((NumericStepper)_modifierControls.First(c => c.ID == "WordsVolume")).Value * 10;
                    commonSave.Options.VoiceVolume = (int)((NumericStepper)_modifierControls.First(c => c.ID == "VoiceVolume")).Value * 10;
                    commonSave.Options.VoiceToggles = (SaveOptions.VoiceOptions)(
                        ((((CheckBox)_modifierControls.First(c => c.ID == "KyonVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.KYON : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "HaruhiVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.HARUHI : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "MikuruVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.MIKURU : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "NagatoVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.NAGATO : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "KoizumiVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.KOIZUMI : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "SisterVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.SISTER : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "TsuruyaVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.TSURUYA : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "TaniguchiVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.TANIGUCHI : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "KunikidaVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.KUNIKIDA : 0) |
                        ((((CheckBox)_modifierControls.First(c => c.ID == "MysteryGirlVoiceToggle")).Checked ?? true) ? (short)SaveOptions.VoiceOptions.MYSTERY_GIRL : 0)
                        );
                    commonSave.Options.StoryOptions = (SaveOptions.PuzzleInvestigationOptions)(
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "BatchDialogueDisplay")).SelectedValue).Text == Application.Instance.Localize(this, "On") ? (short)SaveOptions.PuzzleInvestigationOptions.BATCH_DIALOGUE_DISPLAY_ON : 0) |
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "PuzzleInterruptScenes")).SelectedValue).Text == Application.Instance.Localize(this, "Unseen Only") ? (short)SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY : 0) |
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "PuzzleInterruptScenes")).SelectedValue).Text == Application.Instance.Localize(this, "On") ? (short)SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPTE_SCENES_ON : 0) |
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "TopicStockMode")).SelectedValue).Text == Application.Instance.Localize(this, "On") ? (short)SaveOptions.PuzzleInvestigationOptions.TOPIC_STOCK_MODE_ON : 0) |
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "DialogueSkipping")).SelectedValue).Text == Application.Instance.Localize(this, "Skip Already Read") ? (short)SaveOptions.PuzzleInvestigationOptions.DIALOGUE_SKIPPING_SKIP_ALREADY_READ : 0)
                        );
                }
                else
                {
                    SaveSlotData slotSave = (SaveSlotData)_saveSection;

                    slotSave.SaveTime = ((DateTimePicker)_modifierControls.First(c => c.ID == "DateTime")).Value ?? DateTime.Now;
                    slotSave.ScenarioPosition = (short)((NumericStepper)_modifierControls.First(c => c.ID == "ScenarioPosition")).Value;
                    slotSave.EpisodeNumber = (short)((NumericStepper)_modifierControls.First(c => c.ID == "EpisodeNumber")).Value;
                    slotSave.HaruhiFriendshipLevel = (byte)((NumericStepper)_modifierControls.First(c => c.ID == "HFL")).Value;
                    slotSave.MikuruFriendshipLevel = (byte)((NumericStepper)_modifierControls.First(c => c.ID == "MFL")).Value;
                    slotSave.NagatoFriendshipLevel = (byte)((NumericStepper)_modifierControls.First(c => c.ID == "NFL")).Value;
                    slotSave.KoizumiFriendshipLevel = (byte)((NumericStepper)_modifierControls.First(c => c.ID == "KFL")).Value;
                    slotSave.UnknownFriendshipLevel = (byte)((NumericStepper)_modifierControls.First(c => c.ID == "UFL")).Value;
                    slotSave.TsuruyaFriendshipLevel = (byte)((NumericStepper)_modifierControls.First(c => c.ID == "TFL")).Value;

                    if (slotSave is QuickSaveSlotData quickSave)
                    {
                        quickSave.KbgIndex = (short)(_quickSaveScriptPreview.Kbg?.Id ?? 0);
                        quickSave.Place = (short)(_quickSaveScriptPreview.Place?.Index ?? 0);
                        if (_quickSaveScriptPreview.Background.BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_BG)
                        {
                            quickSave.BgIndex = (short)_quickSaveScriptPreview.Background.Id;
                        }
                        else
                        {
                            Dictionary<ScriptSection, List<ScriptItemCommand>> commandTree = _quickSaveScript.GetScriptCommandTree(_project, _log);
                            ScriptItemCommand currentCommand = commandTree[_quickSaveScript.Event.ScriptSections[((DropDown)_modifierControls.First(c => c.ID == "CurrentScriptBlock")).SelectedIndex]][((NumericMaskedTextBox<int>)_modifierControls.First(c => c.ID == "ScriptCommandIndex")).Value];
                            List<ScriptItemCommand> commands = currentCommand.WalkCommandGraph(commandTree, _quickSaveScript.Graph);
                            for (int i = commands.Count - 1; i >= 0; i--)
                            {
                                if (commands[i].Verb == EventFile.CommandVerb.BG_DISP || commands[i].Verb == EventFile.CommandVerb.BG_DISP2 || (commands[i].Verb == EventFile.CommandVerb.BG_FADE && (((BgScriptParameter)commands[i].Parameters[0]).Background is not null)))
                                {
                                    quickSave.BgIndex = (short)((BgScriptParameter)commands[i].Parameters[0]).Background.Id;
                                }
                            }
                            quickSave.CgIndex = (short)_quickSaveScriptPreview.Background.Id;
                        }
                        quickSave.BgPalEffect = (short)_quickSaveScriptPreview.BgPalEffect;
                        quickSave.EpisodeHeader = _quickSaveScriptPreview.EpisodeHeader;
                        for (int i = 1; i <= 5; i++)
                        {
                            if (_quickSaveScriptPreview.TopScreenChibis.Any(c => c.Chibi.TopScreenIndex == i))
                            {
                                quickSave.TopScreenChibis |= (CharacterMask)(1 << i);
                            }
                        }
                        quickSave.FirstCharacterSprite = _quickSaveScriptPreview.Sprites.ElementAtOrDefault(0).Sprite?.Index ?? 0;
                        quickSave.SecondCharacterSprite = _quickSaveScriptPreview.Sprites.ElementAtOrDefault(1).Sprite?.Index ?? 0;
                        quickSave.ThirdCharacterSprite = _quickSaveScriptPreview.Sprites.ElementAtOrDefault(2).Sprite?.Index ?? 0;
                        quickSave.Sprite1XOffset = (short)(_quickSaveScriptPreview.Sprites.ElementAtOrDefault(0).Positioning?.X ?? 0);
                        quickSave.Sprite2XOffset = (short)(_quickSaveScriptPreview.Sprites.ElementAtOrDefault(1).Positioning?.X ?? 0);
                        quickSave.Sprite3XOffset = (short)(_quickSaveScriptPreview.Sprites.ElementAtOrDefault(2).Positioning?.X ?? 0);
                        quickSave.CurrentScript = int.Parse(((DropDown)_modifierControls.First(c => c.ID == "CurrentScript")).SelectedKey);
                        quickSave.CurrentScriptBlock = ((DropDown)_modifierControls.First(c => c.ID == "CurrentScriptBlock")).SelectedIndex;
                        quickSave.CurrentScriptCommand = ((NumericMaskedTextBox<int>)_modifierControls.First(c => c.ID == "ScriptCommandIndex")).Value;
                    }
                }

                Close();
            };

            Button cancelButton = new() { Text = Application.Instance.Localize(this, "Cancel") };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            StackLayout buttonsLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items =
                {
                    saveButton,
                    cancelButton,
                }
            };

            layout.Rows.Add(buttonsLayout);
            Content = layout;
            Closed += (sender, args) => _callback();
        }

        private TableLayout GetFlagsBox()
        {
            // Add page buttons
            int totalPages = (_flags.Count / FLAGS_PER_PAGE) + 1;
            NumericMaskedTextBox<int> flagPageBox = new()
            {
                Value = _flagPage,
                Width = 30,
            };
            Button lastPageButton = new() { Image = ControlGenerator.GetIcon("Previous_Page", _log), Width = 22, Enabled = _flagPage > 1, };
            lastPageButton.Click += (sender, args) =>
            {
                flagPageBox.Value = _flagPage - 1;
            };
            Button nextPageButton = new() { Image = ControlGenerator.GetIcon("Next_Page", _log), Width = 22, Enabled = _flagPage < totalPages, };
            nextPageButton.Click += (sender, args) =>
            {
                flagPageBox.Value = _flagPage + 1;
            };
            flagPageBox.ValueChanged += (sender, args) =>
            {
                _flagPage = flagPageBox.Value = Math.Max(1, Math.Min(totalPages, flagPageBox.Value));
                lastPageButton.Enabled = _flagPage > 1;
                nextPageButton.Enabled = _flagPage < totalPages;
                UpdateFlagList(_flagPage);
            };

            SearchBox filterBox = new() { PlaceholderText = Application.Instance.Localize(this, "Filter by Name"), Width = 200 };
            CheckBox showSetFlagsCheckbox = new() { Text = Application.Instance.Localize(this, "Hide Unset Flags"), Checked = false };
            Button applyFiltersButton = new() { Image = ControlGenerator.GetIcon("Search", _log), Text = Application.Instance.Localize(this, "Filter") };
            Label totalPagesLabel = new() { Text = $" / {totalPages}" };
            Label totalResultsLabel = new() { Text = string.Format(Application.Instance.Localize(this, "{0} results"), _flags.Count) };
            filterBox.KeyDown += (sender, args) =>
            {
                if (args.Key == Keys.Enter)
                {
                    applyFiltersButton.PerformClick();
                }
            };
            applyFiltersButton.Click += ((sender, args) =>  {
                _flags = GetFilteredFlags(filterBox.Text, showSetFlagsCheckbox.Checked ?? true);
                flagPageBox.Value = _flagPage = 1;
                totalPages = (_flags.Count / FLAGS_PER_PAGE) + 1;
                totalPagesLabel.Text = $" / {totalPages}";
                totalResultsLabel.Text = string.Format(Application.Instance.Localize(this, "{0} results"), _flags.Count);
                UpdateFlagList(_flagPage);
            });

            return new()
            {
                Height = 400,
                Spacing = new(0, 10),
                Padding = 10,
                Rows =
                {
                    GetCenteredLayout([
                        showSetFlagsCheckbox,
                        filterBox,
                        applyFiltersButton,
                    ]),
                    _checkBoxContainer,
                    GetCenteredLayout([totalResultsLabel]),
                    GetCenteredLayout([
                        lastPageButton,
                        flagPageBox,
                        totalPagesLabel,
                        nextPageButton
                    ])
                }
            };
        }

        private static StackLayout GetCenteredLayout(StackLayoutItem[] items)
        {
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    new StackLayout(items)
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Spacing = 5
                    }
                }
            };
        }

        private void UpdateFlagList(int page)
        {
            TableLayout rows = new() { Spacing = new(15, 5) };
            if (_flags.Count > 0)
            {
                rows.Rows.Add(new TableRow
                {
                    Cells =
                    {
                        ControlGenerator.GetTextHeader(Application.Instance.Localize(this, "Value"), 10),
                        ControlGenerator.GetTextHeader(Application.Instance.Localize(this, "Flag Description"), 10)
                    }
                });
            }
            GetFlagBoxes((page - 1) * FLAGS_PER_PAGE, page * FLAGS_PER_PAGE).ToList().ForEach(c => rows.Rows.Add(c));
            _checkBoxContainer.Content = rows;
        }

        private List<TableRow> GetFlagBoxes(int start, int end)
        {
            List<TableRow> flagBoxes = [];
            foreach (int flag in _flags.GetRange(start, Math.Min(end - start, _flags.Count - start)))
            {
                CheckBox flagCheckBox = new() { Checked = _saveSection.IsFlagSet(flag) };
                flagCheckBox.CheckedChanged += (sender, args) => { _flagModifications[flag] = flagCheckBox.Checked ?? true; };
                TableRow checkboxLayout = new()
                {
                    Cells =
                    {
                        flagCheckBox,
                        Flags.GetFlagNickname(flag, _project),
                    }
                };
                flagBoxes.Add(checkboxLayout);
            }
            return flagBoxes;
        }

        private List<int> GetFilteredFlags(string term = null, bool onlyShowSet = false)
        {
            List<int> filtered = [];
            for (int i = 0; i < 5120; i++)
            {
                if (onlyShowSet && !(_flagModifications.TryGetValue(i, out var set) ? set : _saveSection.IsFlagSet(i)))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(term) && !Flags.GetFlagNickname(i, _project).ToLower().Contains(term.ToLower().Trim()))
                {
                    continue;
                }

                filtered.Add(i);
            }
            return filtered;
        }

        private TableLayout GetQuickSaveDataBox(QuickSaveSlotData quickSave)
        {
            StackLayout previewLayout = new();

            NumericMaskedTextBox<int> scriptCommandIndexBox = new() { ID = "ScriptCommandIndex", Value = quickSave.CurrentScriptCommand, Width = 30 };
            _quickSaveScript = (ScriptItem)_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == quickSave.CurrentScript) ?? (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script);
            Dictionary<ScriptSection, List<ScriptItemCommand>> commands = _quickSaveScript.GetScriptCommandTree(_project, _log);
            _quickSaveScript.CalculateGraphEdges(commands, _log);

            DropDown scriptBlockSelector = new() { ID = "CurrentScriptBlock" };
            scriptBlockSelector.Items.AddRange(_quickSaveScript.Event.ScriptSections.Select(s => new ListItem { Key = s.Name, Text = s.Name }));
            scriptBlockSelector.SelectedIndex = quickSave.CurrentScriptBlock;
            scriptBlockSelector.SelectedIndexChanged += (sender, args) =>
            {
                scriptCommandIndexBox.Value = 0;
            };
            _modifierControls.Add(scriptBlockSelector);

            DropDown scriptSelector = new() { ID = "CurrentScript" };
            scriptSelector.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(s => new ListItem { Text = s.DisplayName, Key = ((ScriptItem)s).Event.Index.ToString() }));
            scriptSelector.SelectedKey = quickSave.CurrentScript.ToString();
            StackLayout scriptSelectorWithLink = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Items =
                {
                    ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Script:"), scriptSelector),
                    ControlGenerator.GetFileLink(_quickSaveScript, _tabs, _log),
                }
            };
            scriptSelector.SelectedKeyChanged += (sender, args) =>
            {
                _quickSaveScript = (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == int.Parse(scriptSelector.SelectedKey));
                Dictionary<ScriptSection, List<ScriptItemCommand>> commands = _quickSaveScript.GetScriptCommandTree(_project, _log);
                _quickSaveScript.CalculateGraphEdges(commands, _log);
                scriptBlockSelector.Items.Clear();
                scriptBlockSelector.Items.AddRange(_quickSaveScript.Event.ScriptSections.Select(s => new ListItem { Key = s.Name, Text = s.Name }));
                scriptBlockSelector.SelectedIndex = 0;
                scriptSelectorWithLink.Items.RemoveAt(1);
                scriptSelectorWithLink.Items.Add(ControlGenerator.GetFileLink(_quickSaveScript, _tabs, _log));
            };
            _modifierControls.Add(scriptSelector);

            scriptCommandIndexBox.ValueChanged += (sender, args) =>
            {
                previewLayout.Items.Clear();
                Dictionary<ScriptSection, List<ScriptItemCommand>> commands = _quickSaveScript.GetScriptCommandTree(_project, _log);
                List<ScriptItemCommand> block = commands[_quickSaveScript.Event.ScriptSections[scriptBlockSelector.SelectedIndex < 0 ? 0 : scriptBlockSelector.SelectedIndex]];
                int index = Math.Max(0, Math.Min(block.Count - 1, scriptCommandIndexBox.Value));
                _quickSaveScriptPreview = _quickSaveScript.GetScriptPreview(commands, block[index], _project, _log);
                (SKBitmap scriptPreviewImage, string errorImage) = ScriptItem.GeneratePreviewImage(_quickSaveScriptPreview, _project);
                if (scriptPreviewImage is null)
                {
                    scriptPreviewImage = new(256, 384);
                    SKCanvas canvas = new(scriptPreviewImage);
                    canvas.DrawColor(SKColors.Black);
                    using Stream noPreviewStream = Assembly.GetCallingAssembly().GetManifestResourceStream(errorImage);
                    canvas.DrawImage(SKImage.FromEncodedData(noPreviewStream), new SKPoint(0, 0));
                    canvas.Flush();
                    previewLayout.Items.Add(new SKGuiImage(scriptPreviewImage));
                }
                previewLayout.Items.Add(new SKGuiImage(scriptPreviewImage));
            };
            _modifierControls.Add(scriptCommandIndexBox);

            List<(ChibiItem Chibi, int X, int Y)> topScreenChibis = [];
            int chibiCurrentX = 80;
            int chibiY = 100;
            for (int i = 1; i <= 5; i++)
            {
                if (quickSave.TopScreenChibis.HasFlag((CharacterMask)(1 << i)))
                {
                    ChibiItem chibi = (ChibiItem)_project.Items.First(it => it.Type == ItemDescription.ItemType.Chibi && ((ChibiItem)it).TopScreenIndex == i);
                    topScreenChibis.Add((chibi, chibiCurrentX, chibiY));
                    chibiCurrentX += chibi.ChibiAnimations.First().Value.ElementAt(0).Frame.Width - 16;
                }
            }

            try
            {
                _quickSaveScriptPreview = new()
                {
                    Background = (BackgroundItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == (quickSave.CgIndex != 0 ? quickSave.CgIndex : quickSave.BgIndex)),
                    BgPalEffect = (PaletteEffectScriptParameter.PaletteEffect)quickSave.BgPalEffect,
                    EpisodeHeader = quickSave.EpisodeHeader,
                    Kbg = (BackgroundItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == quickSave.KbgIndex),
                    Place = (PlaceItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Place && ((PlaceItem)i).Index == quickSave.Place),
                    TopScreenChibis = topScreenChibis,
                    Sprites =
                    [
                            new()
                            {
                                Sprite = (CharacterSpriteItem)_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && ((CharacterSpriteItem)i).Index == quickSave.FirstCharacterSprite),
                                Positioning = new() { X = quickSave.Sprite1XOffset, Layer = 2 }
                            },
                        new()
                        {
                            Sprite = (CharacterSpriteItem)_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && ((CharacterSpriteItem)i).Index == quickSave.SecondCharacterSprite),
                            Positioning = new() { X = quickSave.Sprite2XOffset, Layer = 1 }
                        },
                        new()
                        {
                            Sprite = (CharacterSpriteItem)_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && ((CharacterSpriteItem)i).Index == quickSave.ThirdCharacterSprite),
                            Positioning = new() { X = quickSave.Sprite3XOffset, Layer = 0 }
                        },
                    ],
                };
                (SKBitmap previewBitmap, string previewErrorImage) = ScriptItem.GeneratePreviewImage(_quickSaveScriptPreview, _project);
                if (previewBitmap is null)
                {
                    previewBitmap = new(256, 384);
                    SKCanvas canvas = new(previewBitmap);
                    canvas.DrawColor(SKColors.Black);
                    using Stream noPreviewStream = Assembly.GetCallingAssembly().GetManifestResourceStream(previewErrorImage);
                    canvas.DrawImage(SKImage.FromEncodedData(noPreviewStream), new SKPoint(0, 0));
                    canvas.Flush();
                    previewLayout.Items.Add(new SKGuiImage(previewBitmap));
                }
                previewLayout.Items.Add(new SKGuiImage(previewBitmap));
            }
            catch (InvalidOperationException)
            {
                _log.LogWarning("InvalidOperationException encountered while trying to generate quicksave preview; most likely invalid or new save data has been picked...");
            }

            return new TableLayout
            {
                Height = 400,
                Spacing = new(10, 0),
                Padding = 10,
                Rows =
                {
                    new TableRow(
                        new StackLayout()
                        {
                            Orientation = Orientation.Vertical,
                            Spacing = 10,
                            Items =
                            {
                                scriptSelectorWithLink,
                                ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Script Block:"), scriptBlockSelector),
                                ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Command Index:"), scriptCommandIndexBox)
                            }
                        },
                        previewLayout
                    )
                },
            };
        }

        private TableLayout GetSlotDataBox(SaveSlotData slotSave)
        {
            TableLayout layout = new()
            {
                Height = 400,
                Spacing = new(5, 5),
                Padding = 10
            };
            DateTimePicker dateTimePicker = new() { Mode = DateTimePickerMode.DateTime, Value = slotSave.SaveTime.DateTime, ID = "DateTime", MinDate = new DateTime(2000, 1, 1, 0, 0, 0) };
            layout.Rows.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Save Time:"), dateTimePicker));
            _modifierControls.Add(dateTimePicker);

            NumericStepper scenarioPositionStepper = new() { Value = slotSave.ScenarioPosition, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = _project.Scenario.Commands.Count, ID = "ScenarioPosition" };
            layout.Rows.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Scenario Command Index:"), scenarioPositionStepper));
            _modifierControls.Add(scenarioPositionStepper);

            NumericStepper episodeNumberStepper = new() { Value = slotSave.EpisodeNumber, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = 5, ID = "EpisodeNumber" };
            layout.Rows.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Episode:"), episodeNumberStepper));
            _modifierControls.Add(episodeNumberStepper);

            NumericStepper hflStepper = new() { Value = slotSave.HaruhiFriendshipLevel, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = 64, ID = "HFL" };
            _modifierControls.Add(hflStepper);
            NumericStepper mflStepper = new() { Value = slotSave.MikuruFriendshipLevel, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = 64, ID = "MFL" };
            _modifierControls.Add(mflStepper);
            NumericStepper nflStepper = new() { Value = slotSave.NagatoFriendshipLevel, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = 64, ID = "NFL" };
            _modifierControls.Add(nflStepper);
            NumericStepper kflStepper = new() { Value = slotSave.KoizumiFriendshipLevel, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = 64, ID = "KFL" };
            _modifierControls.Add(kflStepper);
            NumericStepper uflStepper = new() { Value = slotSave.UnknownFriendshipLevel, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = 64, ID = "UFL" };
            _modifierControls.Add(uflStepper);
            NumericStepper tflStepper = new() { Value = slotSave.TsuruyaFriendshipLevel, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = 64, ID = "TFL" };
            _modifierControls.Add(tflStepper);
            GroupBox friendshipLevelSteppersGroupBox = new()
            {
                Text = "Friendship Levels",
                Content = new TableLayout
                {
                    Spacing = new(5, 5),
                    Padding = 10,
                    Rows =
                    {
                        new(
                            ControlGenerator.GetCharacterProgressControl(hflStepper, _project, _log, ControlGenerator.ProgressPortraitCharacter.Haruhi),
                            ControlGenerator.GetCharacterProgressControl(mflStepper, _project, _log, ControlGenerator.ProgressPortraitCharacter.Mikuru)
                        ),
                        new(
                            ControlGenerator.GetCharacterProgressControl(nflStepper, _project, _log, ControlGenerator.ProgressPortraitCharacter.Nagato),
                            ControlGenerator.GetCharacterProgressControl(kflStepper, _project, _log, ControlGenerator.ProgressPortraitCharacter.Koizumi)
                        ),
                        new TableRow(
                            ControlGenerator.GetCharacterProgressControl(tflStepper, _project, _log, ControlGenerator.ProgressPortraitCharacter.Tsuruya),
                            ControlGenerator.GetCharacterProgressControl(uflStepper, _project, _log, ControlGenerator.ProgressPortraitCharacter.Unknown)
                        )
                    }
                }
            };
            layout.Rows.Add(new StackLayout(friendshipLevelSteppersGroupBox));
            return layout;
        }

        private TableLayout GetCommonDataBox(CommonSaveData commonSave)
        {
            TableLayout layout = new()
            {
                Height = 400,
                Spacing = new(5, 5),
                Padding = 10
            };

            NumericStepper numSavesStepper = new()
            { Value = commonSave.NumSaves, Increment = 1, MaximumDecimalPlaces = 0, ID = "NumSaves" };
            layout.Rows.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Number of Saves:"), numSavesStepper));
            _modifierControls.Add(numSavesStepper);

            TableLayout volumeLayout = new() { Spacing = new(5, 5), Padding = 10 };
            NumericStepper bgmVolumeStepper = new()
            {
                Value = commonSave.Options.BgmVolume / 10,
                MinValue = 0,
                MaxValue = 100,
                MaximumDecimalPlaces = 0,
                ID = "BgmVolume"
            };
            volumeLayout.Rows.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Music:"), bgmVolumeStepper));
            _modifierControls.Add(bgmVolumeStepper);

            NumericStepper sfxVolumeStepper = new()
            {
                Value = commonSave.Options.SfxVolume / 10,
                MinValue = 0,
                MaxValue = 100,
                MaximumDecimalPlaces = 0,
                ID = "SfxVolume"
            };
            volumeLayout.Rows.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Sound Effects:"), sfxVolumeStepper));
            _modifierControls.Add(sfxVolumeStepper);

            NumericStepper wordsVolumeStepper = new()
            {
                Value = commonSave.Options.WordsVolume / 10,
                MinValue = 0,
                MaxValue = 100,
                MaximumDecimalPlaces = 0,
                ID = "WordsVolume"
            };
            volumeLayout.Rows.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Dialogue:"), wordsVolumeStepper));
            _modifierControls.Add(wordsVolumeStepper);

            NumericStepper voiceVolumeStepper = new()
            {
                Value = commonSave.Options.VoiceVolume / 10,
                MinValue = 0,
                MaxValue = 100,
                MaximumDecimalPlaces = 0,
                ID = "VoiceVolume"
            };
            volumeLayout.Rows.Add(ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Voices:"), voiceVolumeStepper));
            _modifierControls.Add(voiceVolumeStepper);

            CheckBox kyonVoiceCheckBox = new()
            { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.KYON), ID = "KyonVoiceToggle" };
            CheckBox haruhiVoiceCheckBox = new()
            { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.HARUHI), ID = "HaruhiVoiceToggle" };
            CheckBox mikuruVoiceCheckBox = new()
            { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.MIKURU), ID = "MikuruVoiceToggle" };
            CheckBox nagatoVoiceCheckBox = new()
            { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.NAGATO), ID = "NagatoVoiceToggle" };
            CheckBox koizumiVoiceCheckBox = new()
            { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.KOIZUMI), ID = "KoizumiVoiceToggle" };
            CheckBox sisterVoiceCheckBox = new()
            { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.SISTER), ID = "SisterVoiceToggle" };
            CheckBox tsuruyaVoiceCheckBox = new()
            { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.TSURUYA), ID = "TsuruyaVoiceToggle" };
            CheckBox taniguchiVoiceCheckBox = new()
            {
                Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.TANIGUCHI),
                ID = "TaniguchiVoiceToggle"
            };
            CheckBox kunikidaVoiceCheckBox = new()
            { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.KUNIKIDA), ID = "KunikidaVoiceToggle" };
            CheckBox mysteryGirlVoiceCheckBox = new()
            {
                Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.MYSTERY_GIRL),
                ID = "MysteryGirlVoiceToggle"
            };
            TableLayout voiceToggleLayout = new TableLayout
            {
                Spacing = new(5, 5),
                Padding = 10,
                Rows =
                {
                    new(
                        ControlGenerator.GetCharacterVoiceControl(kyonVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Kyon),
                        ControlGenerator.GetCharacterVoiceControl(haruhiVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Haruhi)
                    ),
                    new(
                        ControlGenerator.GetCharacterVoiceControl(mikuruVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Mikuru),
                        ControlGenerator.GetCharacterVoiceControl(nagatoVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Nagato)
                    ),
                    new(
                        ControlGenerator.GetCharacterVoiceControl(tsuruyaVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Tsuruya),
                        ControlGenerator.GetCharacterVoiceControl(koizumiVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Koizumi)
                    ),
                    new(
                        ControlGenerator.GetCharacterVoiceControl(taniguchiVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Taniguchi),
                        ControlGenerator.GetCharacterVoiceControl(sisterVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Sister)
                    ),
                    new(
                        ControlGenerator.GetCharacterVoiceControl(kunikidaVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Kunikida),
                        ControlGenerator.GetCharacterVoiceControl(mysteryGirlVoiceCheckBox, _project, _log, ControlGenerator.VoicePortraitCharacter.Mystery_Girl)
                    )
                }
            };

            _modifierControls.AddRange(
            [
                kyonVoiceCheckBox,
                haruhiVoiceCheckBox,
                mikuruVoiceCheckBox,
                nagatoVoiceCheckBox,
                koizumiVoiceCheckBox,
                sisterVoiceCheckBox,
                tsuruyaVoiceCheckBox,
                taniguchiVoiceCheckBox,
                kunikidaVoiceCheckBox,
                mysteryGirlVoiceCheckBox
            ]);

            RadioButtonList dialogueSkipping = new()
            {
                Items =
                {
                    Application.Instance.Localize(this, "Fast Forward"),
                    Application.Instance.Localize(this, "Skip Already Read")
                },
                SelectedValue =
                    commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions
                        .DIALOGUE_SKIPPING_SKIP_ALREADY_READ)
                        ? Application.Instance.Localize(this, "Skip Already Read")
                        : Application.Instance.Localize(this, "Fast Forward"),
                Orientation = Orientation.Vertical,
                Padding = 10,
                Spacing = new(0, 5),
                ID = "DialogueSkipping",
            };
            _modifierControls.Add(dialogueSkipping);

            RadioButtonList batchDialogueDisplay = new()
            {
                Items =
                {
                    Application.Instance.Localize(this, "Off"),
                    Application.Instance.Localize(this, "On")
                },
                SelectedValue =
                    commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions.BATCH_DIALOGUE_DISPLAY_ON)
                        ? Application.Instance.Localize(this, "On")
                        : Application.Instance.Localize(this, "Off"),
                Orientation = Orientation.Vertical,
                Padding = 10,
                Spacing = new(0, 5),
                ID = "BatchDialogueDisplay",
            };
            _modifierControls.Add(batchDialogueDisplay);

            RadioButtonList puzzleInterruptScenes = new()
            {
                Items =
                {
                    Application.Instance.Localize(this, "Off"),
                    Application.Instance.Localize(this, "Unseen Only"),
                    Application.Instance.Localize(this, "On")
                },
                SelectedValue =
                    commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPTE_SCENES_ON)
                        ? Application.Instance.Localize(this, "On")
                        : (commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions
                            .PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY)
                            ? Application.Instance.Localize(this, "Unseen Only")
                            : Application.Instance.Localize(this, "Off")),
                Orientation = Orientation.Vertical,
                Padding = 10,
                Spacing = new(0, 5),
                ID = "PuzzleInterruptScenes",
            };
            _modifierControls.Add(puzzleInterruptScenes);

            RadioButtonList topicStockMode = new()
            {
                Items =
                {
                    Application.Instance.Localize(this, "Off"),
                    Application.Instance.Localize(this, "On")
                },
                SelectedValue = commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions.TOPIC_STOCK_MODE_ON)
                    ? Application.Instance.Localize(this, "On")
                    : Application.Instance.Localize(this, "Off"),
                Orientation = Orientation.Vertical,
                Padding = 10,
                Spacing = new(0, 5),
                ID = "TopicStockMode",
            };
            _modifierControls.Add(topicStockMode);


            layout.Rows.Add(new TableLayout(
                new TableRow(
                    new GroupBox { Text = Application.Instance.Localize(this, "Volume Config"), Content = volumeLayout },
                    new GroupBox { Text = Application.Instance.Localize(this, "Voices Config"), Content = voiceToggleLayout }
                ),
                new TableRow(
                    new GroupBox { Text = Application.Instance.Localize(this, "Puzzle Interrupt Scenes"), Content = puzzleInterruptScenes },
                    new GroupBox { Text = Application.Instance.Localize(this, "Topic Stock Mode"), Content = topicStockMode }
                ),
                new TableRow(
                    new GroupBox { Text = Application.Instance.Localize(this, "Dialogue Skipping"), Content = dialogueSkipping },
                    new GroupBox { Text = Application.Instance.Localize(this, "Batch Dialogue Display"), Content = batchDialogueDisplay }
                ),
                new TableRow()
            )
            { Spacing = new(10, 0) });

            return layout;
        }
    }

}
