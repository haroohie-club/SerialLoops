﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SkiaSharp;
using static SerialLoops.Lib.Script.Parameters.ScreenScriptParameter;

namespace SerialLoops.Utility
{
    public static partial class SLConverters
    {
        public static FuncValueConverter<ItemDescription.ItemType, Bitmap> ItemTypeToIconConverter => new((type) => new Bitmap(AssetLoader.Open(new Uri($"avares://SerialLoops/Assets/Icons/{type.ToString().Replace(' ', '_')}.png"))));
        public static FuncValueConverter<SKBitmap, SKAvaloniaImage> SKBitmapToAvaloniaConverter => new((bitmap) => new SKAvaloniaImage(bitmap));
        public static FuncValueConverter<DsScreen, bool> TopScreenSelectableConverter => new((screen) => screen != DsScreen.TOP);
        public static FuncValueConverter<DsScreen, bool> BottomScreenSelectableConverter => new((screen) => screen != DsScreen.BOTTOM);
        public static FuncValueConverter<DsScreen, bool> BothScreensSelectedConverter => new((screen) => screen == DsScreen.BOTH);
        public static FuncValueConverter<bool, IImmutableSolidColorBrush> BooleanBrushConverter => new((val) => val ? Brushes.Transparent : Brushes.LightGreen);
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

    public class DoubleSubtractionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value - double.Parse((string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value + double.Parse((string)parameter);
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
}
