using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;

/// Taken from https://github.com/picoe/Eto/blob/ac4610775b70538b96995bdb0c7f0fbcc5ae1b66/test/Eto.Test/SectionList.cs
/// Licensed under the BSD-3 License, reproduced below:
/// 
/*
 * The BSD-3 License.
 * 
 * AUTHORS
 * Copyright © 2011-2022 Curtis Wensley. All Rights Reserved.
 * Copyright © 2012-2013 Vivek Jhaveri. All Rights Reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, are permitted 
 * provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions 
 *    and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions
 *    and the following disclaimer in the documentation and/or other materials provided with the
 *    distribution.
 * 
 * 3. The name of the author may not be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *    
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace SerialLoops.Controls
{
    public interface ISection
    {
        string Text { get; set; }
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
        public Icon SectionIcon { get; set; }
        public string Text { get; set; }

        public Section()
        {
        }

        public Section(string text, IEnumerable<Section> sections, Icon icon)
            : base(sections.OrderBy(r => r.Text, StringComparer.CurrentCultureIgnoreCase).ToArray())
        {
            Text = text;
            SectionIcon = icon;
        }
    }

    /// <summary>
    /// A tree item representation of a section.
    /// </summary>
    public class SectionTreeItem : List<SectionTreeItem>, ITreeGridItem<SectionTreeItem>
    {
        public Section Section { get; private set; }
        public Icon SectionIcon => Section.SectionIcon;
        public string Text => Count > 1 ? $"{Section.Text} ({Count})" : Section.Text;
        public bool Expanded { get; set; }
        public bool Expandable => Count > 0;
        public ITreeGridItem Parent { get; set; }

        public SectionTreeItem(Section section, bool expanded)
        {
            Section = section;
            Expanded = expanded;
            foreach (var child in section)
            {
                SectionTreeItem temp = new(child, expanded) { Parent = this };
                Add(temp); // recursive
            }
        }
    }

    /// <summary>
    /// The base class for nested section lists.
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
                var sectionTreeItem = _treeView.SelectedItem as SectionTreeItem;
                return sectionTreeItem?.Section as ISection;
            }
            set
            {
                _treeView.SelectedItem = FindItem(_treeView.DataStore as SectionTreeItem, value);
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

        public SectionListTreeGridView(IEnumerable<Section> topNodes, Size size, bool expanded)
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
                    ImageBinding = new DelegateBinding<SectionTreeItem, Image>(r => r.SectionIcon),
                    TextBinding = new DelegateBinding<SectionTreeItem, string>(r => r.Text)
                }
            });
            _treeView.SelectedItemChanged += (sender, e) => OnSelectedItemChanged(e);
            _treeView.Activated += (sender, e) => OnActivated(e);
            _treeView.Size = size;
            SetContents(topNodes, expanded);
        }

        public void SetContents(IEnumerable<Section> topNodes, bool expanded)
        {
            _treeView.DataStore = new SectionTreeItem(new Section("Top", topNodes, null), expanded);
        }
    }
}