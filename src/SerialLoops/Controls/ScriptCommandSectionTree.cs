using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Controls
{
    public class ScriptCommandSectionEntry : List<ScriptCommandSectionEntry>, ISection
    {
        public EventFile ScriptFile { get; set; }
        public ScriptItemCommand Command { get; set; }
        public string Text { get; set; }

        public ScriptCommandSectionEntry(ScriptItemCommand command)
            : base(Array.Empty<ScriptCommandSectionEntry>())
        {
            Command = command;
            Text = Command.ToString();
        }

        public ScriptCommandSectionEntry(string text, IEnumerable<ScriptCommandSectionEntry> commands, EventFile scriptFile)
            : base(commands.ToArray())
        {
            Text = text;
            ScriptFile = scriptFile;
        }

        internal ScriptCommandSectionEntry Clone()
        {
            ScriptCommandSectionEntry temp = new(Command.Clone());
            foreach (var child in this)
            {
                temp.Add(child.Clone());
            }
            return temp;
        }
    }

    public class ScriptCommandSectionTreeItem : List<ScriptCommandSectionTreeItem>,
        ITreeGridItem<ScriptCommandSectionTreeItem>
    {
        public ScriptCommandSectionEntry Section { get; private set; }
        public string Text => Section.Text;
        public bool Expanded { get; set; }
        public bool Expandable => Count > 0;
        public ITreeGridItem Parent { get; set; }

        public ScriptCommandSectionTreeItem(ScriptCommandSectionEntry section, bool expanded)
        {
            Section = section;
            Expanded = expanded;
            
            foreach (var child in section)
            {
                ScriptCommandSectionTreeItem temp = new(child, expanded) { Parent = this };
                Add(temp); // recursive
            }
        }

        internal ScriptCommandSectionTreeItem Clone()
        {
            return new(Section.Clone(), Expanded);
        }
    }

    public class ScriptCommandSectionEventArgs : EventArgs
    {
        public int NewIndex { get; set; }
        public ScriptCommandSectionTreeItem NewParent { get; set; }
    }

    public class ScriptCommandSectionTreeGridView : SectionList
    {
        private TreeGridView _treeView;
        private ScriptCommandSectionTreeItem _cursorItem;
        public event EventHandler RepositionCommand;
        public event EventHandler DeleteCommand;
        public event EventHandler<CommandEventArgs> AddCommand;
        public override Control Control => _treeView;

        public override void Focus()
        {
            Control.Focus();
        }

        public override ISection SelectedItem
        {
            get
            {
                var sectionTreeItem = _treeView.SelectedItem as ScriptCommandSectionTreeItem;
                return sectionTreeItem?.Section;
            }
            set
            {
                _treeView.SelectedItem = FindItem(_treeView.DataStore as ScriptCommandSectionTreeItem, value);
            }
        }

        public ScriptCommandSectionTreeItem SelectedCommandTreeItem
        {
            get
            {
                var item = _treeView.SelectedItem;
                if (item is null) return null;
                return item as ScriptCommandSectionTreeItem;            
            }
        }

        ITreeGridItem FindItem(ScriptCommandSectionTreeItem node, ISection section)
        {
            foreach (var item in node)
            {
                if (ReferenceEquals(item.Section, section))
                    return item;
                if (item.Count > 0)
                {
                    var child = FindItem(item, section);
                    if (child != null)
                        return child;
                }
            }
            return null;
        }

        public ScriptCommandSectionTreeGridView(IEnumerable<ScriptCommandSectionEntry> topNodes, Size size, bool expanded)
        {
            _treeView = new TreeGridView
            {
                Style = "sectionList",
                ShowHeader = false,
                AllowEmptySelection = false,
                AllowDrop = true
            };
            _treeView.Columns.Add(new GridColumn
            {
                DataCell = new ImageTextCell
                {
                    TextBinding = new DelegateBinding<ScriptCommandSectionTreeItem, string>(r => r.Text)
                }
            });
            _treeView.SelectedItemChanged += (sender, e) => OnSelectedItemChanged(e);
            _treeView.Activated += (sender, e) => OnActivated(e);
            _treeView.Size = size;
            SetContents(topNodes, expanded);

            _treeView.MouseMove += OnMouseMove;
            _treeView.DragOver += OnDragOver;
            _treeView.DragDrop += OnDragDrop;

            _treeView.ContextMenu = new ScriptCommandListContextMenu(this);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Buttons != MouseButtons.Primary) return;
            if (_treeView.GetCellAt(e.Location).Item is not ScriptCommandSectionTreeItem {Parent: ScriptCommandSectionTreeItem parent} item) return;
            _cursorItem = item;
                
            var data = new DataObject();
            data.SetObject(item, nameof(ScriptCommandSectionTreeItem));
            _treeView.DoDragDrop(data, DragEffects.Move);
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.Contains(nameof(ScriptCommandSectionTreeItem)) || _cursorItem == null) return;
            if (_treeView.GetCellAt(e.Location).Item is not ScriptCommandSectionTreeItem hoveredOver) return;
            if (hoveredOver == _cursorItem) return;
            if (hoveredOver.Parent is not ScriptCommandSectionTreeItem hoveredParent) return;
            if (_cursorItem.Parent is not ScriptCommandSectionTreeItem cursorParent) return;
            if ((hoveredParent.Text == "Top" && cursorParent.Text != "Top") 
                || (cursorParent.Text == "Top" && hoveredParent.Text != "Top")) return;
            e.Effects = DragEffects.Move;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.Contains(nameof(ScriptCommandSectionTreeItem)) || _cursorItem == null) return;
            if (_treeView.GetCellAt(e.Location).Item is not ScriptCommandSectionTreeItem releasedOn) return;
            if (releasedOn == _cursorItem) return;
            if (releasedOn.Parent is not ScriptCommandSectionTreeItem hoveredParent) return;
            if (_cursorItem.Parent is not ScriptCommandSectionTreeItem cursorParent) return;
            if ((hoveredParent.Text == "Top" && cursorParent.Text != "Top") 
                || (cursorParent.Text == "Top" && hoveredParent.Text != "Top")) return;
            var index = hoveredParent.IndexOf(releasedOn);
            if (index == -1) return;

            cursorParent.Remove(_cursorItem);
            hoveredParent.Insert(index, _cursorItem);
            RepositionCommand?.Invoke(this, new ScriptCommandSectionEventArgs { NewIndex = index, NewParent = hoveredParent });
            _treeView.DataStore = _treeView.DataStore;
            _treeView.SelectedItem = _cursorItem;
            _cursorItem = null;
        }

        internal void DeleteItem(ScriptCommandSectionTreeItem item)
        {
            if (item.Parent is not ScriptCommandSectionTreeItem parent) return;
            int newIndex = parent.IndexOf(item);
            parent.Remove(item);
            DeleteCommand?.Invoke(this, EventArgs.Empty);
            _treeView.DataStore = _treeView.DataStore;
            if (newIndex >= parent.Count)
            {
                newIndex = parent.Count - 1;
            }
            if (newIndex < 0) return;
            _treeView.SelectedItem = parent[newIndex];
        }

        internal void AddItem(ScriptCommandSectionTreeItem item, ScriptItemCommand command, ScriptCommandInvocation invocation)
        {
            if (SelectedCommandTreeItem is null) return;
            if (SelectedCommandTreeItem.Parent is ScriptCommandSectionTreeItem parent && !parent.Text.Equals("Top"))
            {
                item.Parent = parent;
                int index = parent.IndexOf(SelectedCommandTreeItem);
                if (index == -1) return;
                parent.Insert(index + 1, item);
                command.Index = index + 1;
            }
            else if (SelectedCommandTreeItem.Parent is ScriptCommandSectionTreeItem top)
            {
                item.Parent = SelectedCommandTreeItem;
                SelectedCommandTreeItem.Insert(0, item);
                command.Index = 0;
            }
            
            AddCommand?.Invoke(this, new(command, invocation)); //todo invoke this with the new item args
            _treeView.SelectedItem = item;
            _treeView.DataStore = _treeView.DataStore;
        }

        internal void AddSection(ScriptCommandSectionTreeItem section)
        {
            ScriptCommandSectionTreeItem rootNode = (ScriptCommandSectionTreeItem)_treeView.DataStore;
            if (rootNode is null) return;
            rootNode.Add(section);
            section.Parent = rootNode;

            AddCommand?.Invoke(this, new(section.Text)); //todo invoke this with the new script section args
            _treeView.SelectedItem = section;
            _treeView.DataStore = rootNode;
        }

        public void SetContents(IEnumerable<ScriptCommandSectionEntry> topNodes, bool expanded)
        {
            _treeView.DataStore = new ScriptCommandSectionTreeItem(new ScriptCommandSectionEntry("Top", topNodes, null), expanded);
        }

        public ScriptCommandSectionEntry FindSection(string text)
        {
            ScriptCommandSectionTreeItem rootNode = (ScriptCommandSectionTreeItem)_treeView.DataStore;
            return rootNode.Find(s => s.Text.Equals(text))?.Section;
        }
    }

    public class CommandEventArgs : EventArgs
    {
        public ScriptItemCommand Command { get; set; }
        public ScriptCommandInvocation Invocation { get; set; }
        public string SectionTitle { get; set; }

        public CommandEventArgs(ScriptItemCommand command, ScriptCommandInvocation invocation)
        {
            Command = command;
            Invocation = invocation;
        }
        public CommandEventArgs(string sectionTitle)
        {
            SectionTitle = sectionTitle;
        }
    }
}