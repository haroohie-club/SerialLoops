using Eto.Forms;
using System;

namespace SerialLoops.Controls
{
    internal class ScriptCommandListContextMenu : ContextMenu
    {
        private readonly ScriptCommandSectionTreeGridView _treeView;
        private ScriptCommandSectionTreeItem _clipboard = null;
        private bool _clipboardIsCut = false;

        public ScriptCommandListContextMenu(ScriptCommandSectionTreeGridView treeView)
        {
            _treeView = treeView;

            Command cutCommand = new();
            cutCommand.Executed += (sender, args) => { };
            Items.Add(new ButtonMenuItem
            {
                Text = "Cut",
                Command = cutCommand,
                Shortcut = Keys.Control | Keys.X
            });

            Command copyCommand = new();
            copyCommand.Executed += (sender, args) => { };
            Items.Add(new ButtonMenuItem
            {
                Text = "Copy",
                Command = copyCommand,
                Shortcut = Keys.Control | Keys.C
            });

            Command pasteCommand = new();
            pasteCommand.Executed += (sender, args) => { };
            Items.Add(new ButtonMenuItem
            {
                Text = "Paste",
                Command = pasteCommand,
                Shortcut = Keys.Control | Keys.V
            });

            Command deleteCommand = new();
            deleteCommand.Executed += (sender, args) => { };
            Items.Add(new ButtonMenuItem
            {
                Text = "Delete",
                Command = deleteCommand,
                Shortcut = Keys.Delete
            });

            cutCommand.Executed += OnCutItem;
            copyCommand.Executed += OnCopyItem;
            pasteCommand.Executed += OnPasteItem;
            deleteCommand.Executed += OnDeleteItem;
        }

        private void OnDeleteItem(object sender, EventArgs e)
        {
            if (_treeView.SelectedItem is not ScriptCommandSectionTreeItem item) return;
            _treeView.DeleteItem(item);
        }

        private void OnPasteItem(object sender, EventArgs e)
        {
            if (_treeView.SelectedItem is not ScriptCommandSectionTreeItem item) return;
            if (_clipboard is null) return;
            if (_clipboardIsCut)
            {
                _treeView.DeleteItem(_clipboard);
            }
            _treeView.AddItem(_clipboard);
        }

        private void OnCopyItem(object sender, EventArgs e)
        {
            CopyItem(false);
        }

        private void OnCutItem(object sender, EventArgs e)
        {
            CopyItem(true);
        }

        private void CopyItem(bool cut)
        {
            if (_treeView.SelectedItem is not ScriptCommandSectionTreeItem item) return;
            _clipboard = item;
            _clipboardIsCut = cut;
        }
    }
}
