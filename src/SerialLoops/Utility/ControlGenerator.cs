using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Utility
{
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
                return new SvgImage { Source = SvgSource.Load(path, new Uri(path)) };
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
                return new Avalonia.Svg.Svg(new Uri($"avares://SerialLoops/Assets/Icons/{iconName}.svg"))
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
    }
}
