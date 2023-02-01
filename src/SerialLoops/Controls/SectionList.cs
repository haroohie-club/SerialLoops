using Eto.Forms;
using System.Collections.Generic;
using System;
using System.Linq;
using Eto.Drawing;

/// Taken from https://github.com/picoe/Eto/blob/ac4610775b70538b96995bdb0c7f0fbcc5ae1b66/test/Eto.Test/SectionList.cs
namespace SerialLoops.Controls
{
    public interface ISection
    {
        string Text { get; }
    }

    /// <summary>
    /// Sections can nest. Each section item can also host
    /// a control that is displayed in the details view when 
    /// the section is selected.
    /// 
    /// Sections do not have any particular visual representation,
    /// and can be wrapped within a tree item (SectionTreeItem) or
    /// any other visual representation.
    /// </summary>
    public class Section : List<Section>, ISection
    {
        public string Text { get; set; }

        public Section()
        {
        }

        public Section(string text, IEnumerable<Section> sections)
            : base(sections.OrderBy(r => r.Text, StringComparer.CurrentCultureIgnoreCase).ToArray())
        {
            Text = text;
        }
    }

    /// <summary>
    /// A tree item representation of a section.
    /// </summary>
    public class SectionTreeItem : List<SectionTreeItem>, ITreeGridItem<SectionTreeItem>
    {
        public Section Section { get; private set; }
        public string Text { get { return Section.Text; } }
        public bool Expanded { get; set; }
        public bool Expandable { get { return Count > 0; } }
        public ITreeGridItem Parent { get; set; }

        public SectionTreeItem(Section section)
        {
            Section = section;
            Expanded = false;
            foreach (var child in section)
            {
                SectionTreeItem temp = new(child);
                temp.Parent = this;
                Add(temp); // recursive
            }
        }
    }

    /// <summary>
    /// The base class for views that display the set of tests.
    /// </summary>
    public abstract class SectionList
    {
        public abstract Control Control { get; }
        public abstract ISection SelectedItem { get; set; }
        public event EventHandler SelectedItemChanged;
        public event EventHandler Activated;

        public string SectionTitle => SelectedItem?.Text;

        protected void OnSelectedItemChanged(EventArgs e)
        {
            SelectedItemChanged?.Invoke(this, e);
        }

        protected void OnActivated(EventArgs e)
        {
            Activated?.Invoke(this, e);
        }

        public abstract void Focus();
    }

    public class SectionListTreeGridView : SectionList
    {
        TreeGridView treeView;

        public override Control Control { get { return treeView; } }

        public override void Focus() { Control.Focus(); }

        public override ISection SelectedItem
        {
            get
            {
                var sectionTreeItem = treeView.SelectedItem as SectionTreeItem;
                return sectionTreeItem?.Section as ISection;
            }
            set
            {
                treeView.SelectedItem = FindItem(treeView.DataStore as SectionTreeItem, value);
            }
        }

        ITreeGridItem FindItem(SectionTreeItem node, ISection section)
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

        public SectionListTreeGridView(IEnumerable<Section> topNodes, Size size)
        {
            treeView = new TreeGridView();
            treeView.Style = "sectionList";
            treeView.ShowHeader = false;
            treeView.AllowEmptySelection = false;
            treeView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = new DelegateBinding<SectionTreeItem, string>(r => r.Text) } });
            treeView.SelectedItemChanged += (sender, e) => OnSelectedItemChanged(e);
            treeView.Activated += (sender, e) => OnActivated(e);
            treeView.DataStore = new SectionTreeItem(new Section("Top", topNodes));
            treeView.Size = size;
        }
    }

}