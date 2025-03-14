using Avalonia.Controls;
using Avalonia.Media;
using SerialLoops.Lib;
using SerialLoops.Utility;

namespace SerialLoops.Models;

public class DialogueColorPalette(Project project) : IColorPalette
{
    public Color GetColor(int colorIndex, int shadeIndex) => project.DialogueColors[colorIndex].ToAvalonia();

    public int ColorCount => project.DialogueColors.Length;
    public int ShadeCount => 1;
}

public class DefaultDialogueColorPalette : IColorPalette
{
    private readonly Color[] _colors =
    [
        new(0xFF, 0xF0, 0xF0, 0xF0),
        new(0xFF, 0xB0, 0xB0, 0x60),
        new(0xFF, 0xD0, 0xD0, 0xF0),
        new(0xFF, 0xB0, 0x90, 0xD0),
        new(0xFF, 0xE0, 0x20, 0x40),
        new(0xFF, 0x70, 0x70, 0x70),
        new(0xFF, 0x00, 0x00, 0x00),
    ];

    public Color GetColor(int colorIndex, int shadeIndex) => _colors[colorIndex];
    public int ColorCount => _colors.Length;
    public int ShadeCount => 1;
}
