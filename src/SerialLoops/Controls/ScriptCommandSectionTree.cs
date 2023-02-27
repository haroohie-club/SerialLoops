using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Lib.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Controls
{
    public class ScriptCommandSectionEntry : List<ScriptCommandSectionEntry>, ISection
    {
        public ScriptItemCommand Command { get; set; }
        public string Text { get; set; }

        public ScriptCommandSectionEntry(ScriptItemCommand command)
            : base(Array.Empty<ScriptCommandSectionEntry>())
        {
            Command = command;
            Text = Command.ToString();
        }

        public ScriptCommandSectionEntry(string text, IEnumerable<ScriptCommandSectionEntry> commands)
            : base(commands.ToArray())
        {
            Text = text;
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
    }

    public class ScriptCommandSectionTreeGridView : SectionList
    {
        private TreeGridView _treeView;
        private ScriptCommandSectionTreeItem _cursorItem;
        public event EventHandler RepositionItem;
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
                return sectionTreeItem?.Section as ISection;
            }
            set
            {
                _treeView.SelectedItem = FindItem(_treeView.DataStore as ScriptCommandSectionTreeItem, value);
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

            // Drag events
            _treeView.MouseMove += OnMouseMove;
            _treeView.DragOver += OnDragOver;
            _treeView.DragDrop += OnDragDrop;
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
            var index = cursorParent.IndexOf(releasedOn);
            if (index == -1) return;

            cursorParent.Remove(_cursorItem);
            cursorParent.Insert(index, _cursorItem);
            RepositionItem?.Invoke(this, e);
            _treeView.DataStore = _treeView.DataStore;
            _treeView.SelectedItem = _cursorItem;
            _cursorItem = null;
        }

        public void SetContents(IEnumerable<ScriptCommandSectionEntry> topNodes, bool expanded)
        {
            _treeView.DataStore = new ScriptCommandSectionTreeItem(new ScriptCommandSectionEntry("Top", topNodes), expanded);
        }
    }
}