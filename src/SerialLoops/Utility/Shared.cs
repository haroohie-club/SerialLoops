using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System.Linq;

namespace SerialLoops.Utility
{
    public static class Shared
    {
        public static void RenameItem(Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log, bool overrideRename = false)
        {
            ItemDescription item = project.FindItem(explorer.Viewer.SelectedItem?.Text);
            if (item is not null)
            {
                if (!item.CanRename && !overrideRename)
                {
                    MessageBox.Show("Can't rename this item directly -- open it to rename it!", "Can't Rename Item", MessageBoxType.Warning);
                    return;
                }
                DocumentPage openTab = tabs.Tabs.Pages.FirstOrDefault(p => p.Text == item.DisplayNameWithStatus);
                ItemRenameDialog renameDialog = new(item, project, log);
                renameDialog.ShowModal();
                explorer.Viewer.SelectedItem.Text = item.DisplayName;
                if (openTab is not null)
                {
                    openTab.Text = item.DisplayNameWithStatus;
                }
                explorer.Invalidate();
                project.ItemNames[item.Name] = item.DisplayName;
                project.Save();
            }
        }
    }
}
