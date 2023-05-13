using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class CharacterItem : Item
    {
        public MessageInfo MessageInfo { get; set; }
        public CharacterInfo CharacterInfo { get; set; }

        public CharacterItem(MessageInfo character, CharacterInfo characterInfo, Project project) : base($"CHR_{project.Characters[(int)character.Character].Name}", ItemType.Character)
        {
            CanRename = false;
            MessageInfo = character;
            CharacterInfo = characterInfo;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }

        public static readonly Dictionary<string, SKColor> BuiltInColors = new()
        {
            { "Kyon", new SKColor(240, 240, 0) },
            { "Haruhi", new SKColor(240, 0, 104) },
            { "Asahina", new SKColor(240, 112, 0) },
            { "Nagato", new SKColor(0, 192, 240) },
            { "Koizumi", new SKColor(172, 96, 248) },
            { "Sister", new SKColor(224, 0, 240) },
            { "Tsuruya", new SKColor(0, 192, 0) },
            { "Taniguchi", new SKColor(200, 104, 104) },
            { "Kunikida", new SKColor(200, 104, 0) },
            { "President", new SKColor(192, 192, 0) },
            { "Other", new SKColor(80, 80, 80) },
            { "Other 2", new SKColor(112, 112, 112) },
            { "Okabe", new SKColor(144, 88, 80) },
            { "Girl", new SKColor(48, 240, 152) },
            { "No Nameplate", new SKColor(0, 0, 0, 0) },
            { "Email", new SKColor(0, 240, 144) },
        };

        public SKBitmap GetNewNameplate(SKBitmap blankNameplate, SKBitmap blankNameplateBaseArrow, Project project, bool transparent = false)
        {
            SKBitmap newNameplate = new(64, 16);
            SKCanvas newCanvas = new(newNameplate);
            newCanvas.DrawBitmap(blankNameplate, new SKPoint(0, 0));

            double widthFactor = 1.0;
            int totalWidth = CharacterInfo.Name.Sum(c =>
            {
                project.FontReplacement.TryGetValue(c, out FontReplacement fr);
                return fr is not null ? fr.Offset : 15;
            });
            if (totalWidth > 53)
            {
                widthFactor = 53.0 / totalWidth;
                totalWidth = 53;
            }
            int currentX = (53 - totalWidth) / 2 + 6, currentY = 1;
            for (int i = 0; i < CharacterInfo.Name.Length; i++)
            {
                if (CharacterInfo.Name[i] != '　') // if it's a space, we just skip drawing
                {
                    int charIndex = project.FontMap.CharMap.IndexOf(CharacterInfo.Name.GetOriginalString(project)[i]);
                    if ((charIndex + 1) * 16 <= project.FontBitmap.Height)
                    {
                        newCanvas.DrawBitmap(project.FontBitmap, new SKRect(0, charIndex * 16, 16, (charIndex + 1) * 16),
                            new SKRect(currentX, currentY, currentX + (int)(16 * widthFactor), currentY + 16), new SKPaint()
                            {
                                ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                                {
                                    CharacterInfo.NameColor.Red / 255.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                                    0.0f, CharacterInfo.NameColor.Green / 255.0f, 0.0f, 0.0f, 0.0f,
                                    0.0f, 0.0f, CharacterInfo.NameColor.Blue / 255.0f, 0.0f, 0.0f,
                                    0.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                                })
                            }
                        );
                    }
                }
                project.FontReplacement.TryGetValue(CharacterInfo.Name[i], out FontReplacement replacement);
                if (replacement is not null && project.LangCode != "ja")
                {
                    currentX += (int)(replacement.Offset * widthFactor);
                }
                else
                {
                    currentX += (int)(14 * widthFactor);
                }
            }
            newCanvas.Flush();

            // Add an outline around the text
            if (CharacterInfo.HasOutline)
            {
                for (int y = 0; y < newNameplate.Height; y++)
                {
                    for (int x = 0; x < newNameplate.Width; x++)
                    {
                        SKColor pixel = newNameplate.GetPixel(x, y);
                        SKColor[] neighborPixels = new SKColor[]
                        {
                        newNameplate.GetPixel(x + 1, y),
                        newNameplate.GetPixel(x - 1, y),
                        newNameplate.GetPixel(x, y + 1),
                        newNameplate.GetPixel(x, y - 1),
                        newNameplate.GetPixel(x + 1, y + 1),
                        newNameplate.GetPixel(x - 1, y + 1),
                        newNameplate.GetPixel(x + 1, y - 1),
                        newNameplate.GetPixel(x - 1, y - 1),
                        };
                        if (Helpers.ColorDistance(pixel, CharacterInfo.NameColor) > 50 && neighborPixels.Any(p => Helpers.ColorDistance(p, CharacterInfo.NameColor) < 50))
                        {
                            newNameplate.SetPixel(x, y, CharacterInfo.OutlineColor);
                        }
                        if (transparent && pixel.Red == 0 && pixel.Green == 128 && pixel.Blue == 0)
                        {
                            newNameplate.SetPixel(x, y, SKColors.Transparent);
                        }
                    }
                }
            }

            // Draw in the remaining components of the nameplate
            newCanvas.DrawBitmap(blankNameplateBaseArrow, new SKPoint(0, 3),
                new SKPaint()
                {
                    ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                    {
                        CharacterInfo.PlateColor.Red / 255.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                        0.0f, CharacterInfo.PlateColor.Green / 255.0f, 0.0f, 0.0f, 0.0f,
                        0.0f, 0.0f, CharacterInfo.PlateColor.Blue / 255.0f, 0.0f, 0.0f,
                        0.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                    })
                });
            newCanvas.DrawLine(new(0, 15), new(59, 15), new() { Color = CharacterInfo.PlateColor });
            newCanvas.Flush();
            return newNameplate;
        }
    }

    public class CharacterInfo
    {
        public string Name { get; set; }
        public SKColor NameColor { get; set; }
        public SKColor PlateColor { get; set; }
        public SKColor OutlineColor { get; set; }
        public bool HasOutline { get; set; }

        public CharacterInfo(string name, SKColor nameColor, SKColor plateColor, SKColor outlineColor, bool hasOutline)
        {
            Name = name;
            NameColor = nameColor;
            PlateColor = plateColor;
            OutlineColor = outlineColor;
            HasOutline = hasOutline;
        }
    }
}
