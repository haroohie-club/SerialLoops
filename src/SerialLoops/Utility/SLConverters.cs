using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SerialLoops.Utility
{
    public static class SLConverters
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
}
