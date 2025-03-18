﻿using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items;

public class CharacterItem : Item
{
    public MessageInfo MessageInfo { get; set; }
    public NameplateProperties NameplateProperties { get; set; }

    public CharacterItem(MessageInfo character, NameplateProperties nameplateProperties, Project project) : base($"CHR_{project.Characters[(int)character.Character].Name}", ItemType.Character)
    {
        CanRename = false;
        MessageInfo = character;
        NameplateProperties = nameplateProperties;
    }

    public override void Refresh(Project project, ILogger log)
    {
    }

    public static readonly Dictionary<string, SKColor> BuiltInColors = new()
    {
        { "Kyon", new(240, 240, 0) },
        { "Haruhi", new(240, 0, 104) },
        { "Asahina", new(240, 112, 0) },
        { "Nagato", new(0, 192, 240) },
        { "Koizumi", new(172, 96, 248) },
        { "Sister", new(224, 0, 240) },
        { "Tsuruya", new(0, 192, 0) },
        { "Taniguchi", new(200, 104, 104) },
        { "Kunikida", new(200, 104, 0) },
        { "President", new(192, 192, 0) },
        { "Other", new(80, 80, 80) },
        { "Other 2", new(112, 112, 112) },
        { "Okabe", new(144, 88, 80) },
        { "Girl", new(48, 240, 152) },
        { "No Nameplate", new(0, 0, 0, 0) },
        { "Email", new(0, 240, 144) },
    };

    public SKBitmap GetNewNameplate(SKBitmap blankNameplate, SKBitmap blankNameplateBaseArrow, Project project, bool transparent = false)
    {
        if (NameplateProperties.Name is null)
        {
            return null;
        }

        SKBitmap newNameplate = new(64, 16);
        SKCanvas newCanvas = new(newNameplate);
        newCanvas.DrawBitmap(blankNameplate, new SKPoint(0, 0));

        double widthFactor = 1.0;
        int totalWidth = NameplateProperties.Name.Sum(c =>
        {
            project.FontReplacement.TryGetValue(c, out FontReplacement fr);
            return fr?.Offset ?? 15;
        });
        if (totalWidth > 53)
        {
            widthFactor = 53.0 / totalWidth;
            totalWidth = 53;
        }
        int currentX = (53 - totalWidth) / 2 + 6, currentY = 1;
        for (int i = 0; i < NameplateProperties.Name.Length; i++)
        {
            if (NameplateProperties.Name[i] != '　') // if it's a space, we just skip drawing
            {
                int charIndex = project.FontMap.CharMap.IndexOf(NameplateProperties.Name.GetOriginalString(project)[i]);
                if ((charIndex + 1) * 16 <= project.FontBitmap.Height)
                {
                    newCanvas.DrawBitmap(project.FontBitmap, new(0, charIndex * 16, 16, (charIndex + 1) * 16),
                        new SKRect(currentX, currentY, currentX + (int)(16 * widthFactor), currentY + 16), new()
                        {
                            ColorFilter = SKColorFilter.CreateColorMatrix(new[]
                            {
                                NameplateProperties.NameColor.Red / 255.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                                0.0f, NameplateProperties.NameColor.Green / 255.0f, 0.0f, 0.0f, 0.0f,
                                0.0f, 0.0f, NameplateProperties.NameColor.Blue / 255.0f, 0.0f, 0.0f,
                                0.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                            })
                        }
                    );
                }
            }
            project.FontReplacement.TryGetValue(NameplateProperties.Name[i], out FontReplacement replacement);
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
        if (NameplateProperties.HasOutline)
        {
            for (int y = 0; y < newNameplate.Height; y++)
            {
                for (int x = 0; x < newNameplate.Width; x++)
                {
                    SKColor pixel = newNameplate.GetPixel(x, y);
                    SKColor[] neighborPixels =
                    [
                        newNameplate.GetPixel(x + 1, y),
                        newNameplate.GetPixel(x - 1, y),
                        newNameplate.GetPixel(x, y + 1),
                        newNameplate.GetPixel(x, y - 1),
                        newNameplate.GetPixel(x + 1, y + 1),
                        newNameplate.GetPixel(x - 1, y + 1),
                        newNameplate.GetPixel(x + 1, y - 1),
                        newNameplate.GetPixel(x - 1, y - 1),
                    ];
                    if (Helpers.ColorDistance(pixel, NameplateProperties.NameColor) > 50 && neighborPixels.Any(p => Helpers.ColorDistance(p, NameplateProperties.NameColor) < 50))
                    {
                        newNameplate.SetPixel(x, y, NameplateProperties.OutlineColor);
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
            new()
            {
                ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
                    NameplateProperties.PlateColor.Red / 255.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                    0.0f, NameplateProperties.PlateColor.Green / 255.0f, 0.0f, 0.0f, 0.0f,
                    0.0f, 0.0f, NameplateProperties.PlateColor.Blue / 255.0f, 0.0f, 0.0f,
                    0.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                })
            });
        newCanvas.DrawLine(new(0, 15), new(59, 15), new() { Color = NameplateProperties.PlateColor });
        newCanvas.Flush();
        return newNameplate;
    }

    public override string ToString() => DisplayName[4..];
}

public class NameplateProperties(string name, SKColor nameColor, SKColor plateColor, SKColor outlineColor, bool hasOutline)
{
    public string Name { get; set; } = name;
    public SKColor NameColor { get; set; } = nameColor;
    public SKColor PlateColor { get; set; } = plateColor;
    public SKColor OutlineColor { get; set; } = outlineColor;
    public bool HasOutline { get; set; } = hasOutline;
}
