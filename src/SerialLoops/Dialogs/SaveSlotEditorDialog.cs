using Eto.Forms;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class SaveSlotEditorDialog : FloatingForm
    {
        private readonly ILogger _log;
        private readonly SaveSection _saveSection;
        private readonly Project _project;
        private readonly EditorTabsPanel _tabs;

        private readonly Dictionary<int, bool> _flagModifications = [];
        private readonly List<Control> _modifierControls = [];

        public SaveSlotEditorDialog(ILogger log, SaveSection saveSection, Project project, EditorTabsPanel tabs)
        {
            _log = log;
            _saveSection = saveSection;
            _project = project;
            _tabs = tabs;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Save Slot Editor";
            Width = 500;
            Height = 800;

            StackLayout flagsLayout = new()
            {
                Orientation = Orientation.Vertical,
                Padding = 4,
                Spacing = 4,
                Width = 500,
            };
            for (int i = 0; i < 5120; i++)
            {
                int flagId = i;
                CheckBox flagCheckBox = new() { Checked = _saveSection.IsFlagSet(flagId) };
                flagCheckBox.CheckedChanged += (sender, args) =>
                {
                    if (_flagModifications.ContainsKey(flagId))
                    {
                        _flagModifications[flagId] = flagCheckBox.Checked ?? true;
                    }
                    else
                    {
                        _flagModifications.Add(flagId, flagCheckBox.Checked ?? true);
                    }
                };
                StackLayout checkboxLayout = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 2,
                    Items =
                    {
                        flagCheckBox,
                        Flags.GetFlagNickname(flagId, _project),
                    }
                };

                flagsLayout.Items.Add(checkboxLayout);
            }

            TableLayout layout = new()
            {
                Spacing = new(5, 5),
                Rows =
                {
                    new TableRow(new Scrollable { Content = flagsLayout, Height = 400 }),
                }
            };

            if (_saveSection is CommonSaveData commonSave)
            {
                NumericStepper numSavesStepper = new() { Value = commonSave.NumSaves, Increment = 1, MaximumDecimalPlaces = 0, ID = "NumSaves" };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Number of Saves: ", numSavesStepper));
                _modifierControls.Add(numSavesStepper);

                NumericStepper bgmVolumeStepper = new() { Value = commonSave.Options.BgmVolume / 10, MinValue = 0, MaxValue = 100, MaximumDecimalPlaces = 0, ID = "BgmVolume" };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("BGM Volume: ", bgmVolumeStepper));
                _modifierControls.Add(bgmVolumeStepper);

                NumericStepper sfxVolumeStepper = new() { Value = commonSave.Options.SfxVolume / 10, MinValue = 0, MaxValue = 100, MaximumDecimalPlaces = 0, ID = "SfxVolume" };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("SFX Volume: ", sfxVolumeStepper));
                _modifierControls.Add(sfxVolumeStepper);

                NumericStepper wordsVolumeStepper = new() { Value = commonSave.Options.WordsVolume / 10, MinValue = 0, MaxValue = 100, MaximumDecimalPlaces = 0, ID = "WordsVolume" };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Words Volume: ", wordsVolumeStepper));
                _modifierControls.Add(wordsVolumeStepper);

                NumericStepper voiceVolumeStepper = new() { Value = commonSave.Options.VoiceVolume / 10, MinValue = 0, MaxValue = 100, MaximumDecimalPlaces = 0, ID = "VoiceVolume" };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Voice Volume: ", voiceVolumeStepper));
                _modifierControls.Add(voiceVolumeStepper);

                CheckBox kyonVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.KYON), ID = "KyonVoiceToggle" };
                CheckBox haruhiVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.HARUHI), ID = "HaruhiVoiceToggle" };
                CheckBox mikuruVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.MIKURU), ID = "MikuruVoiceToggle" };
                CheckBox nagatoVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.NAGATO), ID = "NagatoVoiceToggle" };
                CheckBox koizumiVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.KOIZUMI), ID = "KoizumiVoiceToggle" };
                CheckBox sisterVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.SISTER), ID = "SisterVoiceToggle" };
                CheckBox tsuruyaVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.TSURUYA), ID = "TsuruyaVoiceToggle" };
                CheckBox taniguchiVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.TANIGUCHI), ID = "TaniguchiVoiceToggle" };
                CheckBox kunikidaVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.KUNIKIDA), ID = "KunikidaVoiceToggle" };
                CheckBox mysteryGirlVoiceCheckBox = new() { Checked = commonSave.Options.VoiceToggles.HasFlag(SaveOptions.VoiceOptions.MYSTERY_GIRL), ID = "MysteryGirlVoiceToggle" };
                TableLayout voiceToggleLayout = new()
                {
                    Spacing = new(3, 3),
                    Rows =
                    {
                        new(ControlGenerator.GetControlWithLabel("Kyon", kyonVoiceCheckBox), ControlGenerator.GetControlWithLabel("Haruhi", haruhiVoiceCheckBox),
                            ControlGenerator.GetControlWithLabel("Mikuru", mikuruVoiceCheckBox), ControlGenerator.GetControlWithLabel("Nagato", nagatoVoiceCheckBox),
                            ControlGenerator.GetControlWithLabel("Koizumi", koizumiVoiceCheckBox)),
                        new(ControlGenerator.GetControlWithLabel("Kyon's Sister", sisterVoiceCheckBox), ControlGenerator.GetControlWithLabel("Tsuruya", tsuruyaVoiceCheckBox),
                            ControlGenerator.GetControlWithLabel("Taniguchi", taniguchiVoiceCheckBox), ControlGenerator.GetControlWithLabel("Kunikida", kunikidaVoiceCheckBox),
                            ControlGenerator.GetControlWithLabel("Mystery Girl", mysteryGirlVoiceCheckBox))
                    }
                };
                layout.Rows.Add("Voice Toggles");
                layout.Rows.Add(voiceToggleLayout);
                _modifierControls.AddRange(
                    [kyonVoiceCheckBox,
                    haruhiVoiceCheckBox,
                    mikuruVoiceCheckBox,
                    nagatoVoiceCheckBox,
                    koizumiVoiceCheckBox,
                    sisterVoiceCheckBox,
                    tsuruyaVoiceCheckBox,
                    taniguchiVoiceCheckBox,
                    kunikidaVoiceCheckBox,
                    mysteryGirlVoiceCheckBox]);

                RadioButtonList batchDialogueDisplay = new()
                {
                    Items =
                    {
                        "Off",
                        "On"
                    },
                    SelectedValue = commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions.BATCH_DIALOGUE_DISPLAY_ON) ? "On" : "Off",
                    Orientation = Orientation.Horizontal,
                    ID = "BatchDialogueDisplay",
                };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Batch Dialogue Display", batchDialogueDisplay));
                _modifierControls.Add(batchDialogueDisplay);

                RadioButtonList puzzleInterruptScenes = new()
                {
                    Items =
                    {
                        "Off",
                        "Unseen Only",
                        "On"
                    },
                    SelectedValue = commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPTE_SCENES_ON) ? "On" : 
                        (commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY) ? "Unseen Only" : "Off"),
                    Orientation = Orientation.Horizontal,
                    ID = "PuzzleInterruptScenes",
                };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Puzzle Interrupt Scenes", puzzleInterruptScenes));
                _modifierControls.Add(puzzleInterruptScenes);

                RadioButtonList topicStockMode = new()
                {
                    Items =
                    {
                        "Off",
                        "On"
                    },
                    SelectedValue = commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions.TOPIC_STOCK_MODE_ON) ? "On" : "Off",
                    Orientation = Orientation.Horizontal,
                    ID = "TopicStockMode",
                };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Topic Stock Mode", topicStockMode));
                _modifierControls.Add(topicStockMode);

                RadioButtonList dialogueSkipping = new()
                {
                    Items =
                    {
                        "Fast Forward",
                        "Skip Already Read"
                    },
                    SelectedValue = commonSave.Options.StoryOptions.HasFlag(SaveOptions.PuzzleInvestigationOptions.DIALOGUE_SKIPPING_SKIP_ALREADY_READ) ? "Skip Already Read" : "Fast Forward",
                    Orientation = Orientation.Horizontal,
                    ID = "DialogueSkipping",
                };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Dialogue Skipping", dialogueSkipping));
                _modifierControls.Add(dialogueSkipping);
            }
            else
            {
                SaveSlotData slotSave = (SaveSlotData)_saveSection;

                NumericStepper scenarioPositionStepper = new() { Value = slotSave.ScenarioPosition, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = _project.Scenario.Commands.Count, ID = "ScenarioPosition" };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Scenario Command Index: ", scenarioPositionStepper));
                _modifierControls.Add(scenarioPositionStepper);

                NumericStepper episodeNumberStepper = new() { Value = slotSave.EpisodeNumber, Increment = 1, MaximumDecimalPlaces = 0, MinValue = 1, MaxValue = 5, ID = "EpisodeNumber" };
                layout.Rows.Add(ControlGenerator.GetControlWithLabel("Episode: ", episodeNumberStepper));
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
                        Rows =
                        {
                            new(ControlGenerator.GetControlWithLabel("Haruhi", hflStepper), ControlGenerator.GetControlWithLabel("Mikuru", mflStepper), ControlGenerator.GetControlWithLabel("Nagato", nflStepper)),
                            new(ControlGenerator.GetControlWithLabel("Koizumi", kflStepper), ControlGenerator.GetControlWithLabel("Unknown", uflStepper), ControlGenerator.GetControlWithLabel("Tsuruya", tflStepper)),
                        }
                    }
                };
                layout.Rows.Add(friendshipLevelSteppersGroupBox);

                if (slotSave is QuickSaveSlotData quickSave)
                {
                    NumericMaskedTextBox<int> scriptCommandIndexBox = new() { ID = "ScriptCommandIndex", Value = quickSave.CurrentScriptCommand };
                    _modifierControls.Add(scriptCommandIndexBox);

                    ScriptItem script = (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == quickSave.CurrentScript);
                    DropDown scriptBlockSelector = new() { ID = "CurrentScriptBlock" };
                    scriptBlockSelector.Items.AddRange(script.Event.ScriptSections.Select(s => new ListItem { Key = s.Name, Text = s.Name }));
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
                            ControlGenerator.GetControlWithLabel("Current Script: ", scriptSelector),
                            ControlGenerator.GetFileLink(script, _tabs, _log),
                        }
                    };
                    scriptSelector.SelectedKeyChanged += (sender, args) =>
                    {
                        ScriptItem newScript = (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == int.Parse(scriptSelector.SelectedKey));
                        scriptBlockSelector.Items.Clear();
                        scriptBlockSelector.Items.AddRange(newScript.Event.ScriptSections.Select(s => new ListItem { Key = s.Name, Text = s.Name }));
                        scriptBlockSelector.SelectedIndex = 0;
                        scriptSelectorWithLink.Items.RemoveAt(1);
                        scriptSelectorWithLink.Items.Add(ControlGenerator.GetFileLink(newScript, _tabs, _log));
                    };
                    _modifierControls.Add(scriptSelector);

                    layout.Rows.Add(scriptSelectorWithLink);
                    layout.Rows.Add(ControlGenerator.GetControlWithLabel("Current Script Block: ", scriptBlockSelector));
                    layout.Rows.Add(ControlGenerator.GetControlWithLabel("Current Script Command Index: ", scriptCommandIndexBox));
                }
            }

            Button saveButton = new() { Text = "Save" };
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
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "BatchDialogueDisplay")).SelectedValue).Text == "On" ? (short)SaveOptions.PuzzleInvestigationOptions.BATCH_DIALOGUE_DISPLAY_ON : 0) |
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "PuzzleInterruptScenes")).SelectedValue).Text == "Unseen Only" ? (short)SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY : 0) |
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "PuzzleInterruptScenes")).SelectedValue).Text == "On" ? (short)SaveOptions.PuzzleInvestigationOptions.PUZZLE_INTERRUPTE_SCENES_ON : 0) |
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "TopicStockMode")).SelectedValue).Text == "On" ? (short)SaveOptions.PuzzleInvestigationOptions.TOPIC_STOCK_MODE_ON : 0) |
                        (((ListItem)((RadioButtonList)_modifierControls.First(c => c.ID == "DialogueSkipping")).SelectedValue).Text == "Skip Already Read" ? (short)SaveOptions.PuzzleInvestigationOptions.DIALOGUE_SKIPPING_SKIP_ALREADY_READ : 0)
                        );
                }
                else
                {
                    SaveSlotData slotSave = (SaveSlotData)_saveSection;

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
                        quickSave.CurrentScript = int.Parse(((DropDown)_modifierControls.First(c => c.ID == "CurrentScript")).SelectedKey);
                        quickSave.CurrentScriptBlock = ((DropDown)_modifierControls.First(c => c.ID == "CurrentScriptBlock")).SelectedIndex;
                        quickSave.CurrentScriptCommand = ((NumericMaskedTextBox<int>)_modifierControls.First(c => c.ID == "ScriptCommandIndex")).Value;
                    }
                }

                Close();
            };

            Button cancelButton = new() { Text = "Cancel" };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            StackLayout buttonsLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items =
                {
                    saveButton,
                    cancelButton,
                }
            };

            layout.Rows.Add(buttonsLayout);

            Content = layout;
        }
    }
}
