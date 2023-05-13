using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Topten.RichTextKit;

namespace SerialLoops.Editors
{
    public class PlaceEditor : Editor
    {
        private PlaceItem _place;

        public PlaceEditor(PlaceItem placeItem, Project project, ILogger log) : base(placeItem, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _place = (PlaceItem)Description;

            TextBox placeTextBox = new() { Text = _place.DisplayName[4..] };

            StackLayout previewPanel = new()
            {
                Items =
                {
                    new SKGuiImage(_place.GetPreview(_project)),
                },
            };

            using Stream typefaceStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SerialLoops.Graphics.MS-Gothic-Haruhi.ttf");
            SKTypeface msGothicHaruhi = SKTypeface.FromStream(typefaceStream);
            if (!CustomFontMapper.HasFont())
            {
                CustomFontMapper.AddFont(msGothicHaruhi);
            }

            placeTextBox.TextChanged += (sender, args) =>
            {
                string spaceAdjustedText = placeTextBox.Text.Replace(" ", "   ");
                SKBitmap newPlaceBitmap = new(_place.PlaceGraphic.Width, _place.PlaceGraphic.Height);
                SKCanvas canvas = new(newPlaceBitmap);
                canvas.DrawRegion(new(new SKRectI(0, 0, newPlaceBitmap.Width, newPlaceBitmap.Height)), new SKPaint { Color = new SKColor(0, 249, 0) });
                TextBlock placeText = new()
                {
                    Alignment = Topten.RichTextKit.TextAlignment.Left,
                    FontMapper = new CustomFontMapper(),
                    MaxWidth = newPlaceBitmap.Width - 2,
                    MaxHeight = newPlaceBitmap.Height - 12,
                };
                placeText.AddText(spaceAdjustedText, new Style()
                {
                    TextColor = SKColors.Black,
                    FontFamily = msGothicHaruhi.FamilyName,
                    FontSize = 15.0f,
                    LetterSpacing = -1,
                    HaloColor = new SKColor(160, 160, 160),
                    HaloBlur = 0,
                    HaloWidth = 4,
                });
                TextBlock placeTextShadow = new()
                {
                    Alignment = Topten.RichTextKit.TextAlignment.Left,
                    FontMapper = new CustomFontMapper(),
                    MaxWidth = newPlaceBitmap.Width - 2,
                    MaxHeight = newPlaceBitmap.Height - 12,
                };
                placeTextShadow.AddText(spaceAdjustedText, new Style()
                {
                    TextColor = SKColors.Black,
                    FontFamily = msGothicHaruhi.FamilyName,
                    FontSize = 15.0f,
                    LetterSpacing = -1,
                    HaloColor = new SKColor(88, 88, 88),
                    HaloBlur = 0,
                    HaloWidth = 4,
                });
                placeTextShadow.Paint(canvas, new SKPoint(2, 7), new() { Edging = SKFontEdging.Alias });
                placeText.Paint(canvas, new SKPoint(1, 6), new() { Edging = SKFontEdging.Antialias });
                canvas.Flush();

                previewPanel.Items.Clear();
                previewPanel.Items.Add(new SKGuiImage(newPlaceBitmap));

                UpdateTabTitle(false);
            };

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Place Name", placeTextBox),
                    previewPanel,
                }
            };
        }

        private class CustomFontMapper : FontMapper
        {
            private static Dictionary<string, SKTypeface> _fonts = new();

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
