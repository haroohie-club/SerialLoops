using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;

namespace SerialLoops.Controls
{
    internal class ItemContextMenu : ContextMenu
    {

        private readonly ILogger _log;
        private readonly Project _project;

        private readonly EditorTabsPanel _tabs;
        private readonly ItemExplorerPanel _explorer;

        public ItemContextMenu(Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log) : base()
        {
            _project = project;
            _tabs = tabs;
            _explorer = explorer;
            _log = log;

            Command openCommand = new();
            openCommand.Executed += OpenCommand_OnClick;
            Items.Add(new ButtonMenuItem { Text = "Open", Command = openCommand });

            Command findReferences = new();
            findReferences.Executed += FindReferences_OnClick;
            Items.Add(new ButtonMenuItem { Text = "Find References...", Command = findReferences });

            Command findReferencedBy = new();
            findReferencedBy.Executed += FindReferencedBy_OnClick;
            Items.Add(new ButtonMenuItem { Text = "Find Referenced By...", Command = findReferencedBy });
        }

        private void OpenCommand_OnClick(object sender, EventArgs args)
        {
            ItemDescription item = _project.FindItem(_explorer.Viewer.SelectedItem?.Text);
            if (item != null)
            {
                _tabs.OpenTab(item, _log);
            }
        }

        private void FindReferences_OnClick(object sender, EventArgs args)
        {
            ShowReferences(ReferenceDialog.ReferenceMode.REFERENCES_TO);
        }

        private void FindReferencedBy_OnClick(object sender, EventArgs args)
        {
            ShowReferences(ReferenceDialog.ReferenceMode.REFERENCED_BY);
        }

        private void ShowReferences(ReferenceDialog.ReferenceMode mode)
        {
            ItemDescription item = _project.FindItem(_explorer.Viewer.SelectedItem?.Text.Split(" - ")[0]);
            if (item != null)
            {
                ReferenceDialog referenceDialog = new(item, mode, _project, _explorer, _tabs, _log);
                referenceDialog.ShowModal(_explorer);
            }
        }
        
    }
}
