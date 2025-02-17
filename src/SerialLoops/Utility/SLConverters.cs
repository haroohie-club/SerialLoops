using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
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
    public static FuncValueConverter<ItemDescription.ItemType, Bitmap> ItemTypeToIconConverter => new((type) => new(AssetLoader.Open(new($"avares://SerialLoops/Assets/Icons/{type.ToString().Replace(' ', '_')}.png"))));
    public static FuncValueConverter<SKBitmap, SKAvaloniaImage> SKBitmapToAvaloniaConverter => new((bitmap) => new(bitmap));
    public static FuncValueConverter<DsScreen, bool> TopScreenSelectableConverter => new((screen) => screen != DsScreen.TOP);
    public static FuncValueConverter<DsScreen, bool> BottomScreenSelectableConverter => new((screen) => screen != DsScreen.BOTTOM);
    public static FuncValueConverter<DsScreen, bool> BothScreensSelectedConverter => new((screen) => screen == DsScreen.BOTH);
    public static FuncValueConverter<bool, IImmutableSolidColorBrush> BooleanBrushConverter => new((val) => val ? Brushes.Transparent : Brushes.LightGreen);
    public static FuncValueConverter<string, string> CharacterNameCropConverter => new((name) => name[4..]);
    public static FuncValueConverter<List<Speaker>, string> ListDisplayConverter => new((strs) => string.Join(", ", strs.Select(s => s.ToString())));
}

public class DisplayNameConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not UnsetValueType && values[1] is not UnsetValueType)
        {
            string displayName = (string)values[0];
            bool unsavedChanges = (bool)values[1];

            return unsavedChanges ? $"* {displayName}" : displayName;
        }
        return string.Empty;
    }
}

public class TextSubstitionConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not UnsetValueType && values[1] is not UnsetValueType)
        {
            string originalText = (string)values[0];
            Project project = (Project)values[1];

            return originalText.GetSubstitutedString(project);
        }
        return string.Empty;
    }
}

public class SKAvaloniaColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((SKColor)value).ToAvalonia();
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((Color)value).ToSKColor();
    }
}

public class SKAvaloniaBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return new SolidColorBrush(((SKColor)value).ToAvalonia());
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((SolidColorBrush)value).Color.ToSKColor();
    }
}

public class IntGreaterThanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType == typeof(int))
        {
            return (int)value > int.Parse((string)parameter);
        }
        if (targetType == typeof(short))
        {
            return (short)value > int.Parse((string)parameter);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return 0;
    }
}

public class ShortSubtractionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (short)value - short.Parse((string)parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (short)value + short.Parse((string)parameter);
    }
}

public class IntSubtractionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value - int.Parse((string)parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value + int.Parse((string)parameter);
    }
}

public class IntAdditionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value + int.Parse((string)parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value - int.Parse((string)parameter);
    }
}

public class BgmLoopSampleToTimestampConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not UnsetValueType && values[1] is not UnsetValueType)
        {
            BgmLoopPreviewItem loopPreview = (BgmLoopPreviewItem)values[0];
            uint sample = (uint)values[1];

            return (decimal)loopPreview.GetTimestampFromSample(sample);
        }
        return (decimal)0.0;
    }
}

// borrowed implementation from: https://github.com/AvaloniaUI/Avalonia/discussions/11902
public class GapClipMaskConverter : IMultiValueConverter
{
    public static readonly GapClipMaskConverter Instance = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (values is not [Rect bounds, Rect gap]
            || bounds == default
            || gap == default)
        {
            return new BindingNotification(
                new ArgumentException(
                    "GapClipMaskConverter expects two non-empty rectangles (type Avalonia.Rect)."),
                BindingErrorType.Error);
        }

        gap = bounds.Intersect(gap);

        return new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new RectangleGeometry { Rect = new(bounds.Size) },
            new RectangleGeometry { Rect = new(gap.Position - bounds.Position, gap.Size) });
    }
}
