using System.Collections.Generic;
using Avalonia.Controls;

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
    }
}
