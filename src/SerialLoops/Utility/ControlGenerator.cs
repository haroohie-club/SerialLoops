using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;

namespace SerialLoops.Utility;

public static class ControlGenerator
{
    public static Bitmap GetIcon(string iconName, ILogger log, int size = 100)
    {
        try
        {
            return new Bitmap(AssetLoader.Open(new($"avares://SerialLoops/Assets/Icons/{iconName}.png")))
                .CreateScaledBitmap(new(size, size));
        }
        catch (Exception ex)
        {
            log.LogWarning($"Failed to load icon '{iconName}': {ex.Message}\n\n{ex.StackTrace}");
            return null;
        }
    }

    public static SvgImage GetVectorIcon(string iconName, ILogger log)
    {
        try
        {
            var path = $"avares://SerialLoops/Assets/Icons/{iconName}.svg";
            return new() { Source = SvgSource.Load(path, new(path)) };
        }
        catch (Exception ex)
        {
            log.LogWarning($"Failed to load icon '{iconName}': {ex.Message}\n\n{ex.StackTrace}");
            return null;
        }
    }

    public static Avalonia.Svg.Svg GetVectorIcon(string iconName, ILogger log, int size = 100)
    {
        try
        {
            return new(new Uri($"avares://SerialLoops/Assets/Icons/{iconName}.svg"))
            {
                Path = $"avares://SerialLoops/Assets/Icons/{iconName}.svg",
                Width = size,
                Height = size
            };
        }
        catch (Exception ex)
        {
            log.LogWarning($"Failed to load icon '{iconName}': {ex.Message}\n\n{ex.StackTrace}");
            return null;
        }
    }

    public static string GetVectorPath(string iconName)
    {
        return $"avares://SerialLoops/Assets/Icons/{iconName}.svg";
    }

    public static string LocalizeItemTypes(ItemDescription.ItemType type)
    {
        return type switch
        {
            ItemDescription.ItemType.Background => Strings.Backgrounds,
            ItemDescription.ItemType.BGM => Strings.BGMs,
            ItemDescription.ItemType.Character => Strings.Characters,
            ItemDescription.ItemType.Character_Sprite => Strings.Character_Sprites,
            ItemDescription.ItemType.Chess_Puzzle => Strings.Chess_Puzzles,
            ItemDescription.ItemType.Chibi => Strings.Chibis,
            ItemDescription.ItemType.Group_Selection => Strings.Group_Selections,
            ItemDescription.ItemType.Item => Strings.Items,
            ItemDescription.ItemType.Layout => Strings.Layouts,
            ItemDescription.ItemType.Map => Strings.Maps,
            ItemDescription.ItemType.Place => Strings.Places,
            ItemDescription.ItemType.Puzzle => Strings.Puzzles,
            ItemDescription.ItemType.Scenario => Strings.Scenario,
            ItemDescription.ItemType.Script => Strings.Scripts,
            ItemDescription.ItemType.SFX => Strings.SFXs,
            ItemDescription.ItemType.System_Texture => Strings.System_Textures,
            ItemDescription.ItemType.Topic => Strings.Topics,
            ItemDescription.ItemType.Transition => Strings.Transitions,
            ItemDescription.ItemType.Voice => Strings.Voices,
            _ => "UNKNOWN TYPE",
        };
    }

    public static StackPanel GetControlWithIcon(Control control, string iconName, ILogger log)
    {
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 5,
        };
        panel.Children.Add(GetVectorIcon(iconName, log, size: 16));
        panel.Children.Add(control);
        return panel;
    }

    internal static TextBlock GetTextHeader(string text, int size = 14)
    {
        return new() { Text = text, FontWeight = FontWeight.Bold, FontSize = size };
    }
}
