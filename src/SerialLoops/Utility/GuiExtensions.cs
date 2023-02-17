using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;

namespace SerialLoops.Utility
{
    public static class GuiExtensions
    {
        public static ContextMenu GetContextMenu(this string itemName, Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log)
        {
            return project.FindItem(itemName) is not null ? new ItemContextMenu(project, explorer, tabs, log) : new TypeContextMenu(project, explorer, tabs, log);
        }
    }
}
