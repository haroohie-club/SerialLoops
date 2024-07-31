using System.Collections.Generic;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using Topten.RichTextKit;

namespace SerialLoops.Lib.Items
{
    public class PlaceItem : Item, IPreviewableGraphic
    {
        public int Index { get; set; }
        public GraphicsFile PlaceGraphic { get; set; }
        public string PlaceName { get; set; }

        public PlaceItem(int index, GraphicsFile placeGrp) : base(placeGrp.Name[0..^3], ItemType.Place)
        {
            Index = index;
            CanRename = false;
            PlaceGraphic = placeGrp;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }

        public static SKBitmap Unscramble(SKBitmap placeGraphic)
        {
            SKBitmap adjustedPlace = new(placeGraphic.Width, placeGraphic.Height);
            SKCanvas canvas = new(adjustedPlace);

            int col = 0, row = 0;
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    canvas.DrawBitmap(placeGraphic,
                        new SKRect(x * placeGraphic.Width / 2, y * placeGraphic.Height / 4, (x + 1) * placeGraphic.Width / 2, (y + 1) * placeGraphic.Height / 4),
                        new SKRect(col * placeGraphic.Width / 2, row * placeGraphic.Height / 4, (col + 1) * placeGraphic.Width / 2, (row + 1) * placeGraphic.Height / 4));
                    row++;
                    if (row >= 4)
                    {
                        row = 0;
                        col++;
                    }
                }
            }

            canvas.Flush();
            return adjustedPlace;
        }

        public SKBitmap GetPreview(Project project)
        {
            return Unscramble(PlaceGraphic.GetImage(transparentIndex: 0));
        }

        public SKBitmap GetNewPlaceGraphic(SKTypeface msGothicHaruhi)
        {
            string spaceAdjustedText = PlaceName.Replace(" ", "   ");
            SKBitmap newPlaceBitmap = new(PlaceGraphic.Width, PlaceGraphic.Height);
            SKCanvas canvas = new(newPlaceBitmap);
            SKColor bgColor = new(0, 249, 0);
            canvas.DrawRegion(new(new SKRectI(0, 0, newPlaceBitmap.Width, newPlaceBitmap.Height)), new SKPaint { Color = bgColor });
            TextBlock placeText = new()
            {
                Alignment = TextAlignment.Left,
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
                Alignment = TextAlignment.Left,
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
            placeText.Paint(canvas, new SKPoint(1, 6), new() { Edging = SKFontEdging.SubpixelAntialias });
            canvas.Flush();

            // Antialiasing creates some semitransparent pixels on top of our green background, which causes them to render as green
            // rather than as transparent. To prevent this, we forcibly set them back to the transparent color
            for (int y = 0; y < newPlaceBitmap.Height; y++)
            {
                for (int x = 0; x < newPlaceBitmap.Width; x++)
                {
                    if (Helpers.ColorDistance(newPlaceBitmap.GetPixel(x, y), bgColor) < 350)
                    {
                        newPlaceBitmap.SetPixel(x, y, bgColor);
                    }
                }
            }

            return newPlaceBitmap;
        }

        public class CustomFontMapper : FontMapper
        {
            private static Dictionary<string, SKTypeface> _fonts = [];

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
