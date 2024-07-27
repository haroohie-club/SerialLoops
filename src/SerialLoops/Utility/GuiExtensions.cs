using Avalonia.Controls;
using System.Collections.Generic;

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
    }
}
