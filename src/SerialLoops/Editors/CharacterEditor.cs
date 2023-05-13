using Eto.Forms;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Topten.RichTextKit;

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

            TextBox characterBox = new() { Text = _character.CharacterInfo.Name };

            ColorPicker textColorPicker = new() { Value = _character.CharacterInfo.NameColor.ToEtoDrawingColor() };
            ColorPicker plateColorPicker = new() { Value = _character.CharacterInfo.PlateColor.ToEtoDrawingColor() };
            ColorPicker outlineColorPicker = new() { Value = _character.CharacterInfo.OutlineColor.ToEtoDrawingColor() };
            CheckBox outlineCheckBox = new() { Checked = _character.CharacterInfo.HasOutline };

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
                _character.CharacterInfo.Name = characterBox.Text;
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
                UpdateTabTitle(false);
            };
            textColorPicker.ValueChanged += (sender, args) =>
            {
                _character.CharacterInfo.NameColor = textColorPicker.Value.ToSKColor();
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
                UpdateTabTitle(false);
            };
            plateColorPicker.ValueChanged += (sender, args) =>
            {
                _character.CharacterInfo.PlateColor = plateColorPicker.Value.ToSKColor();
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
                _character.CharacterInfo.OutlineColor = outlineColorPicker.Value.ToSKColor();
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
                UpdateTabTitle(false);
            };
            outlineCheckBox.CheckedChanged += (sender, args) =>
            {
                _character.CharacterInfo.HasOutline = outlineCheckBox.Checked ?? true;
                UpdatePreview(nameplatePreviewLayout, blankNameplate, blankNameplateBaseArrow);
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
                    ControlGenerator.GetControlWithLabel("Voice Font", new NumericStepper { Value = _character.MessageInfo.VoiceFont }),
                    ControlGenerator.GetControlWithLabel("Text Timer", new NumericStepper { Value = _character.MessageInfo.TextTimer }),
                },
            };
        }

        private void UpdatePreview(StackLayout nameplatePreviewLayout, SKBitmap blankNameplate, SKBitmap blankNameplateBaseArrow)
        {
            nameplatePreviewLayout.Items.Clear();
            nameplatePreviewLayout.Items.Add(new SKGuiImage(_character.GetNewNameplate(blankNameplate, blankNameplateBaseArrow, _project)));
        }
        private class CustomFontMapper : FontMapper
        {
            private static readonly Dictionary<string, SKTypeface> _fonts = new();

            public static void AddFont(SKTypeface typeface)
            {
                _fonts.Add(typeface.FamilyName, typeface);
            }

            public static bool HasFont()
            {
                return _fonts.Count > 0;
            }

            public override SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants)
            {
                return _fonts[style.FontFamily];
            }
        }
    }
}
