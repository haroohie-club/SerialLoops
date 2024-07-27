using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using HaruhiChokuretsuLib.Util;
using System;
using System.Reflection;

namespace SerialLoops.Utility
{
    public static class ControlGenerator
    {
        public static Bitmap GetIcon(string iconName, ILogger log, int size = 16)
        {
            try
            {
                return new Bitmap(Assembly.GetCallingAssembly().GetManifestResourceStream($"SerialLoops.Assets.Icons.{iconName}.png")).CreateScaledBitmap(new(size, size));
            }
            catch (Exception ex)
            {
                log.LogWarning($"Failed to load icon '{iconName}': {ex.Message}\n\n{ex.StackTrace}");
                return null;
            }
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
            panel.Children.Add(new Image { Source = GetIcon(iconName, log) });
            panel.Children.Add(control);
            return panel;
        }
    }
}
