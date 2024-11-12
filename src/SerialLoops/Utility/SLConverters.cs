using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SkiaSharp;
using static SerialLoops.Lib.Script.Parameters.ScreenScriptParameter;

namespace SerialLoops.Utility;

public static partial class SLConverters
{
    public static FuncValueConverter<ItemDescription.ItemType, Bitmap> ItemTypeToIconConverter => new((type) => new Bitmap(AssetLoader.Open(new Uri($"avares://SerialLoops/Assets/Icons/{type.ToString().Replace(' ', '_')}.png"))));
    public static FuncValueConverter<SKBitmap?, SKAvaloniaImage?> SKBitmapToAvaloniaConverter => new((bitmap) => bitmap is null ? null : new(bitmap));
    public static FuncValueConverter<DsScreen, bool> TopScreenSelectableConverter => new((screen) => screen != DsScreen.TOP);
    public static FuncValueConverter<DsScreen, bool> BottomScreenSelectableConverter => new((screen) => screen != DsScreen.BOTTOM);
    public static FuncValueConverter<DsScreen, bool> BothScreensSelectedConverter => new((screen) => screen == DsScreen.BOTH);
    public static FuncValueConverter<bool, IImmutableSolidColorBrush> BooleanBrushConverter => new((val) => val ? Brushes.Transparent : Brushes.LightGreen);
    public static FuncValueConverter<string, string> CharacterNameCropConverter => new((name) => name![4..]);
    public static FuncValueConverter<List<Speaker>, string> ListDisplayConverter => new((strs) => string.Join(", ", strs?.Select(s => s.ToString()) ?? []));
}

public class DisplayNameConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is string displayName && values[1] is bool unsavedChanges)
        {
            return unsavedChanges ? $"* {displayName}" : displayName;
        }
        return string.Empty;
    }
}

public class SKAvaloniaColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ((SKColor?)value ?? SKColor.Empty).ToAvalonia();
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ((Color?)value ?? new Color()).ToSKColor();
    }
}

public class DoubleSubtractionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (double?)value ?? 0 - double.Parse((string?)parameter ?? "0");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (double?)value ?? 0 + double.Parse((string?)parameter ?? "0");
    }
}

public class IntSubtractionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (int?)value ?? 0 - int.Parse((string?)parameter ?? "0");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (int?)value ?? 0 + int.Parse((string?)parameter ?? "0");
    }
}

public class IntAdditionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (int?)value ?? 0 + int.Parse((string?)parameter ?? "0");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (int?)value ?? 0 - int.Parse((string?)parameter ?? "0");
    }
}

public class BgmLoopSampleToTimestampConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is BgmLoopPreviewItem loopPreview && values[1] is uint sample)
        {
            return (decimal)loopPreview.GetTimestampFromSample(sample);
        }
        return (decimal)0.0;
    }
}
