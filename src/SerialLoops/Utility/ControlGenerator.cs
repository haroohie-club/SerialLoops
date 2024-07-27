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
    }
}
