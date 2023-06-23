using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SerialLoops.Editors
{
    public class CharacterEditor : Editor
    {
        private CharacterItem _character;

        public CharacterEditor(CharacterItem dialogueConfig, Project project, ILogger log) : base(dialogueConfig, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _character = (CharacterItem)Description;

            TextBox characterBox = new() { Text = _character.NameplateProperties.Name };

            ColorPicker textColorPicker = new() { Value = _character.NameplateProperties.NameColor.ToEtoDrawingColor() };
            ColorPicker plateColorPicker = new() { Value = _character.NameplateProperties.PlateColor.ToEtoDrawingColor() };
            ColorPicker outlineColorPicker = new() { Value = _character.NameplateProperties.OutlineColor.ToEtoDrawingColor() };
            CheckBox outlineCheckBox = new() { Checked = _character.NameplateProperties.HasOutline };

            DropDown defaultTextColorsDropDown = new();
            DropDown defaultPlateColorsDropDown = new();
            defaultTextColorsDropDown.Items.AddRange(CharacterItem.BuiltInColors.Keys.Select(c => new ListItem { Key = c, Text = c }));
            defaultPlateColorsDropDown.Items.AddRange(CharacterItem.BuiltInColors.Keys.Select(c => new ListItem { Key = c, Text = c }));

            SKBitmap nameplatePreview = new(64, 16);
            using SKCanvas baseCanvas = new(nameplatePreview);
            baseCanvas.DrawBitmap(_project.NameplateBitmap, new SKRect(0, 16 * ((int)_character.MessageInfo.Character - 1), 64, 16 * ((int)_character.MessageInfo.Character)), new SKRect(0, 0, 64, 16));
            baseCanvas.Flush();
            StackLayout nameplatePreviewLayout = new()
            {
                Items =
                {
                    new SKGuiImage(nameplatePreview),
                },
            };

            using Stream blankNameplateStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SerialLoops.Graphics.BlankNameplate.png");
            SKBitmap blankNameplate = SKBitmap.Decode(blankNameplateStream);
            using Stream blankNameplateBaseArrowStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SerialLoops.Graphics.BlankNameplateBaseArrow.png");
            SKBitmap blankNameplateBaseArrow = SKBitmap.Decode(blankNameplateBaseArrowStream);

            characterBox.TextChanged += (sender, args) =>
            {
                _character.NameplateProperties.Name = characterBox.Text;
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
                UpdateTabTitle(false);
            };
            textColorPicker.ValueChanged += (sender, args) =>
            {
                _character.NameplateProperties.NameColor = textColorPicker.Value.ToSKColor();
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
                UpdateTabTitle(false);
            };
            plateColorPicker.ValueChanged += (sender, args) =>
            {
                _character.NameplateProperties.PlateColor = plateColorPicker.Value.ToSKColor();
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
                UpdateTabTitle(false);
            };
            defaultTextColorsDropDown.SelectedKeyChanged += (sender, args) =>
            {
                textColorPicker.Value = CharacterItem.BuiltInColors[defaultTextColorsDropDown.SelectedKey].ToEtoDrawingColor();
            };
            defaultPlateColorsDropDown.SelectedKeyChanged += (sender, args) =>
            {
                plateColorPicker.Value = CharacterItem.BuiltInColors[defaultPlateColorsDropDown.SelectedKey].ToEtoDrawingColor();
            };
            outlineColorPicker.ValueChanged += (sender, args) =>
            {
                _character.NameplateProperties.OutlineColor = outlineColorPicker.Value.ToSKColor();
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
                UpdateTabTitle(false);
            };
            outlineCheckBox.CheckedChanged += (sender, args) =>
            {
                _character.NameplateProperties.HasOutline = outlineCheckBox.Checked ?? true;
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
                UpdateTabTitle(false);
            };

            if (_character.MessageInfo.Character == HaruhiChokuretsuLib.Archive.Event.Speaker.MONOLOGUE || _character.MessageInfo.Character == HaruhiChokuretsuLib.Archive.Event.Speaker.INFO)
            {
                characterBox.Enabled = false;
                textColorPicker.Enabled = false;
                plateColorPicker.Enabled = false;
                defaultTextColorsDropDown.Enabled = false;
                defaultPlateColorsDropDown.Enabled = false;
                outlineColorPicker.Enabled = false;
                outlineCheckBox.Enabled = false;
            }

            NumericStepper voiceFontStepper = new()
            {
                Value = _character.MessageInfo.VoiceFont,
                MinValue = 0,
                MaxValue = short.MaxValue,
                DecimalPlaces = 0,
            };
            voiceFontStepper.ValueChanged += (sender, args) =>
            {
                _character.MessageInfo.VoiceFont = (short)voiceFontStepper.Value;
                _project.MessInfo.MessageInfos.FirstOrDefault(m => _character.MessageInfo.Character == m.Character).VoiceFont = (short)voiceFontStepper.Value;
                UpdateTabTitle(false);
            };

            return new StackLayout
            {
                Spacing = 5,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Character", characterBox),
                    ControlGenerator.GetControlWithLabel("Text Color", new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 3,
                        Items =
                        {
                            textColorPicker,
                            ControlGenerator.GetControlWithLabel("Defaults", defaultTextColorsDropDown),
                        }
                    }),
                    ControlGenerator.GetControlWithLabel("Plate Color", new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 3,
                        Items =
                        {
                            plateColorPicker,
                            ControlGenerator.GetControlWithLabel("Defaults", defaultPlateColorsDropDown),
                        }
                    }),
                    ControlGenerator.GetControlWithLabel("Outline Color", new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 3,
                        Items =
                        {
                            outlineColorPicker,
                            ControlGenerator.GetControlWithLabel("Has Outline?", outlineCheckBox),
                        }
                    }),
                    nameplatePreviewLayout,
                    ControlGenerator.GetControlWithLabel("Voice Font", voiceFontStepper),
                    ControlGenerator.GetControlWithLabel("Text Timer", new NumericStepper { Value = _character.MessageInfo.TextTimer, Enabled = false }),
                },
            };
        }

        private void UpdatePreview(StackLayout nameplatePreviewLayout, SKBitmap blankNameplate, SKBitmap blankNameplateBaseArrow)
        {
            nameplatePreviewLayout.Items.Clear();
            nameplatePreviewLayout.Items.Add(new SKGuiImage(_character.GetNewNameplate(blankNameplate, blankNameplateBaseArrow, _project)));
        }
    }
}
