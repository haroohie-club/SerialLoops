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
            ItemDescription.ItemType.Background => Strings.ItemsPanelBackgrounds,
            ItemDescription.ItemType.BGM => Strings.ItemsPanelBGMs,
            ItemDescription.ItemType.Character => Strings.ItemsPanelCharacters,
            ItemDescription.ItemType.Character_Sprite => Strings.ItemsPanelCharacterSprites,
            ItemDescription.ItemType.Chess_Puzzle => Strings.ItemsPanelChessPuzzles,
            ItemDescription.ItemType.Chibi => Strings.ItemsPanelChibis,
            ItemDescription.ItemType.Group_Selection => Strings.ItemsPanelGroupSelections,
            ItemDescription.ItemType.Item => Strings.ItemsPanelItems,
            ItemDescription.ItemType.Layout => Strings.ItemsPanelLayouts,
            ItemDescription.ItemType.Map => Strings.ItemsPanelMaps,
            ItemDescription.ItemType.Place => Strings.ItemsPanelPlaces,
            ItemDescription.ItemType.Puzzle => Strings.ItemsPanelPuzzles,
            ItemDescription.ItemType.Scenario => Strings.ItemsPanelScenario,
            ItemDescription.ItemType.Script => Strings.ItemsPanelScripts,
            ItemDescription.ItemType.SFX => Strings.ItemsPanelSFXs,
            ItemDescription.ItemType.System_Texture => Strings.ItemsPanelSystemTextures,
            ItemDescription.ItemType.Topic => Strings.ItemsPanelTopics,
            ItemDescription.ItemType.Transition => Strings.ItemsPanelTransitions,
            ItemDescription.ItemType.Voice => Strings.ItemsPanelVoices,
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
