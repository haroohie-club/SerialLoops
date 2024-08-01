using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;

namespace SerialLoops.Utility
{
    public static class GuiExtensions
    {
        public static void AddRange(this ItemCollection itemCollection, IEnumerable<ContentControl> items)
        {
            foreach (ContentControl item in items)
            {
                itemCollection.Add(item);
            }
        }

        public static void AddRange(this Avalonia.Controls.Controls controlsCollection, IEnumerable<Control> controlsToAdd)
        {
            foreach (Control control in controlsToAdd)
            {
                controlsCollection.Add(control);
            }
        }

        public static NativeMenuItem FindNativeMenuItem(this NativeMenu menu, string header)
        {
            foreach (NativeMenuItemBase itemBase in menu.Items)
            {
                if (itemBase is NativeMenuItem item)
                {
                    if (item.Header.Equals(header))
                    {
                        return item;
                    }
                    else
                    {
                        if (item.Menu?.FindNativeMenuItem(header) is not null)
                        {
                            return item;
                        }
                    }
                }
            }
            return null;
        }

        public static Bitmap ToAvaloniaBitmap(this SKBitmap skBitmap)
        {
            using MemoryStream memStream = new();
            skBitmap.Encode(SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            Bitmap bitmap = new(memStream);
            return bitmap;
        }
    }
}
