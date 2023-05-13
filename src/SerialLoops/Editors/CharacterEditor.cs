﻿using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

            SKBitmap nameplatePreview = new(64, 16);
            using SKCanvas baseCanvas = new(nameplatePreview);
            baseCanvas.DrawBitmap(_project.SpeakerBitmap, new SKRect(0, 16 * ((int)_character.MessageInfo.Character - 1), 64, 16 * ((int)_character.MessageInfo.Character)), new SKRect(0, 0, 64, 16));
            baseCanvas.Flush();
            StackLayout nameplatePreviewLayout = new()
            {
                Items =
                {
                    new SKGuiImage(nameplatePreview),
                },
            };

            using Stream fontStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SerialLoops.Graphics.msgothic-variablewidth.ttf");
            SKTypeface font = SKTypeface.FromStream(fontStream);
            if (!CustomFontMapper.HasFont())
            {
                CustomFontMapper.AddFont(font);
            }
            SKFont trueFont = new(font, 13.0f);
            float[] widths = new float[26];
            SKRect[] bounds = new SKRect[26];
            trueFont.GetGlyphWidths(font.GetGlyphs("abcdefghijklmnopqrstuvwxyz"), widths, bounds);
            _log.Log(string.Join(", ", widths));
            _log.Log(string.Join(", ", bounds));

            using Stream blankNameplateStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SerialLoops.Graphics.BlankNameplate.png");
            SKBitmap blankNameplate = SKBitmap.Decode(blankNameplateStream);
            using Stream blankNameplateBaseArrowStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SerialLoops.Graphics.BlankNameplateBaseArrow.png");
            SKBitmap blankNameplateBaseArrow = SKBitmap.Decode(blankNameplateBaseArrowStream);

            characterBox.TextChanged += (sender, args) =>
            {
                SKBitmap newNameplate = new(64, 16);
                SKCanvas newCanvas = new(newNameplate);
                newCanvas.DrawBitmap(blankNameplate, new SKPoint(0, 0));
                newCanvas.DrawBitmap(blankNameplateBaseArrow, new SKPoint(0, 3),
                    new SKPaint()
                    {
                        ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                        {
                            plateColorPicker.Value.ToSKColor().Red / 255.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                            0.0f, plateColorPicker.Value.ToSKColor().Green / 255.0f, 0.0f, 0.0f, 0.0f,
                            0.0f, 0.0f, plateColorPicker.Value.ToSKColor().Blue / 255.0f, 0.0f, 0.0f,
                            0.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                        })
                    });
                newCanvas.DrawLine(new(0, 15), new(59, 15), new() { Color = plateColorPicker.Value.ToSKColor() });
                TextBlock textBlock = new() { Alignment = Topten.RichTextKit.TextAlignment.Center, FontMapper = new CustomFontMapper(), MaxWidth = 52, MaxHeight = 13 };
                textBlock.AddText(characterBox.Text,
                    new Style()
                    {
                        TextColor = textColorPicker.Value.ToSKColor(),
                        FontFamily = font.FamilyName,
                        FontSize = 13.0f,
                        HaloWidth = 0.5f,
                        HaloColor = new SKColor(128, 128, 128),
                    });
                textBlock.Paint(newCanvas, new SKPoint(7, 2), new() { Edging = SKFontEdging.SubpixelAntialias, SubpixelPositioning = true });
                newCanvas.Flush();
                nameplatePreviewLayout.Items.Clear();
                nameplatePreviewLayout.Items.Add(new SKGuiImage(newNameplate));
            };

            return new StackLayout
            {
                Spacing = 5,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Character", characterBox),
                    nameplatePreviewLayout,
                    ControlGenerator.GetControlWithLabel("Voice Font", new NumericStepper { Value = _character.MessageInfo.VoiceFont }),
                    ControlGenerator.GetControlWithLabel("Text Timer", new NumericStepper { Value = _character.MessageInfo.TextTimer }),
                },
            };
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

        private class CustomStyle : Style
        {

        }
    }
}
