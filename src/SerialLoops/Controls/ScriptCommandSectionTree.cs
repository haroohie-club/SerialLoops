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
    public class ScriptCommandSectionTreeItem : List<ScriptCommandSectionTreeItem>, ITreeGridItem<ScriptCommandSectionTreeItem>
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
                AllowEmptySelection = false
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
        }

        public void SetContents(IEnumerable<ScriptCommandSectionEntry> topNodes, bool expanded)
        {
            _treeView.DataStore = new ScriptCommandSectionTreeItem(new ScriptCommandSectionEntry("Top", topNodes), expanded);
        }
    }
}
