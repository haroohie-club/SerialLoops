using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using SerialLoops.Models;

namespace SerialLoops.Utility
{
    public static partial class SLConverters
    {
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

    public class BgmLoopSampleToTimestampConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not UnsetValueType && values[1] is not UnsetValueType)
            {
                BgmLoopPreviewItem loopPreview = (BgmLoopPreviewItem)values[0];
                uint sample = (uint)values[1];

                return loopPreview.GetTimestampFromSample(sample);
            }
            return 0.0;
        }
    }
}
