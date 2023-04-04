using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.IO;
using System.Linq;

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
            Items.Add(new ButtonMenuItem {Text = "Open", Command = openCommand});

            Command findReferences = new();
            findReferences.Executed += FindReferences_OnClick;
            Items.Add(new ButtonMenuItem {Text = "Find References...", Command = findReferences});
        }

        private void OpenCommand_OnClick(object sender, EventArgs args)
        {
            ItemDescription item = _project.FindItem(_explorer.Viewer.SelectedItem?.Text);
            if (item is not null)
            {
                _tabs.OpenTab(item, _log);
            }
        }

        private void FindReferences_OnClick(object sender, EventArgs args)
        {
            ItemDescription item = _project.FindItem(_explorer.Viewer.SelectedItem?.Text);
            if (item != null)
            {
                ReferencesDialog referenceDialog = new(item, _project, _explorer, _tabs, _log);
                referenceDialog.ShowModal(_explorer);
            }
        }
    }

    public class TypeContextMenu : ContextMenu
    {
        private readonly ILogger _log;
        private readonly Project _project;

        private readonly EditorTabsPanel _tabs;
        private readonly ItemExplorerPanel _explorer;

        public TypeContextMenu(Project project, ItemExplorerPanel explorer, EditorTabsPanel tabs, ILogger log) : base()
        {
            _project = project;
            _tabs = tabs;
            _explorer = explorer;
            _log = log;

            Command exportNames = new();
            exportNames.Executed += ExportNames_OnClick;
            Items.Add(new ButtonMenuItem {Text = "Export Item Names", Command = exportNames});
        }

        private void ExportNames_OnClick(object sender, EventArgs e)
        {
            ItemDescription item = _project.FindItem(_explorer.Viewer.SelectedItem?.Text);
            ItemDescription.ItemType type;
            if (item is null && _explorer.Viewer.SelectedItem is not null)
            {
                type = Enum.Parse<ItemDescription.ItemType>(_explorer.Viewer.SelectedItem.Text[0..^1]
                    .Replace(' ', '_'));
            }
            else
            {
                type = item.Type;
            }

            string[] names = _project.Items.Where(i => i.Type == type).Select(i => i.Name).ToArray();
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filters.Add(new("File Names List", ".txt"));
            if (saveFileDialog.ShowAndReportIfFileSelected(_tabs))
            {
                File.WriteAllLines(saveFileDialog.FileName, names);
            }
        }
    }

    public class ScriptCommandListContextMenu : ContextMenu
    {
        private readonly ScriptCommandSectionTreeGridView _treeView;
        private ScriptCommandSectionTreeItem _clipboard;
        private bool _clipboardIsCut;
        
        public Command CutCommand { get; }
        public Command CopyCommand { get; }
        public Command PasteCommand { get; }
        public Command DeleteCommand { get; }

        public ScriptCommandListContextMenu(ScriptCommandSectionTreeGridView treeView, ILogger log)
        {
            _treeView = treeView;

            CutCommand = new()
            {
                MenuText = "Cut",
                ToolBarText = "Cut",
                Image = ControlGenerator.GetIcon("Cut", log),
                Shortcut = Keys.Control | Keys.X
            };
            CutCommand.Executed += OnCutItem;

            CopyCommand = new()
            {
                MenuText = "Copy",
                ToolBarText = "Copy",
                Image = ControlGenerator.GetIcon("Copy", log),
                Shortcut = Keys.Control | Keys.C
            };
            CopyCommand.Executed += OnCopyItem;

            PasteCommand = new()
            {
                MenuText = "Paste",
                ToolBarText = "Paste",
                Image = ControlGenerator.GetIcon("Paste", log),
                Shortcut = Keys.Control | Keys.V
            };
            PasteCommand.Executed += OnPasteItem;
            

            DeleteCommand = new()
            {
                MenuText = "Delete",
                ToolBarText = "Delete",
                Image = ControlGenerator.GetIcon("Remove", log),
                Shortcut = Keys.Delete
            };
            DeleteCommand.Executed += OnDeleteItem;
            
            Items.Add(new ButtonMenuItem(CutCommand));
            Items.Add(new ButtonMenuItem(CopyCommand));
            Items.Add(new ButtonMenuItem(PasteCommand));
            Items.Add(new ButtonMenuItem(DeleteCommand));
        }

        private void OnDeleteItem(object sender, EventArgs e)
        {
            if (_treeView.SelectedCommandTreeItem is null) return;
            _treeView.DeleteItem(_treeView.SelectedCommandTreeItem);
        }

        private void OnPasteItem(object sender, EventArgs e)
        {
            if (_treeView.SelectedCommandTreeItem is null) return;
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
            if (_treeView.SelectedCommandTreeItem is null) return;
            _clipboard = _treeView.SelectedCommandTreeItem.Clone();
            _clipboardIsCut = cut;
        }
    }
}