using Eto.Forms;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class SaveSlotEditorDialog : Dialog
    {
        private ILogger _log;
        private SaveSection _saveSection;
        private Project _project;

        private readonly Dictionary<int, bool> _flagModifications = [];
        private readonly List<Control> _modifierControls = [];

        public SaveSlotEditorDialog(ILogger log, SaveSection saveSection, Project project)
        {
            _log = log;
            _saveSection = saveSection;
            _project = project;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Width = 500;
            Height = 700;

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


            }
            else if (_saveSection is DynamicSaveSlotData dynamicSave)
            {

            }
            else
            {
                SaveSlotData staticSave = (SaveSlotData)_saveSection;
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
