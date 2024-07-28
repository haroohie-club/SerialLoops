using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Input;
using Avalonia.Layout;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SerialLoops.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace SerialLoops.Models
{
    public abstract class ItemListPanel : ViewModelBase
    {
        public double Width { get; set; }
        public double Height { get; set; }

        private List<ItemDescription> _items;
        public List<ItemDescription> Items
        {
            protected get { return _items; }
            set
            {
                _items = value;
                Source = new HierarchicalTreeDataGridSource<ITreeItem>(GetSections())
                {
                    Columns =
                    {
                        new HierarchicalExpanderColumn<ITreeItem>(
                            new TextColumn<ITreeItem, string>("Text", i => i.Text),
                            i => i.Children),
                    }
                };
            }
        }

        public HierarchicalTreeDataGridSource<ITreeItem> Source { get; set; }
        public TreeDataGrid Viewer { get; set; }

        protected ILogger _log;
        protected bool ExpandItems { get; set; }

        public void InitializeBase(List<ItemDescription> items, TreeDataGrid viewer, Size size, bool expandItems, ILogger log)
        {
            Viewer = viewer;
            Items = items;
            Width = size.Width;
            Height = size.Height;
            ExpandItems = expandItems;
            _log = log;
            Viewer.DoubleTapped += ItemList_ItemDoubleClicked;
        }

        private ObservableCollection<ITreeItem> GetSections()
        {
            return new ObservableCollection<ITreeItem>(Items.GroupBy(i => i.Type).OrderBy(g => g.Key)
                .Select(g => new SectionTreeItem($"{g.Key}s", g.Select(i => new ItemDescriptionTreeItem(i)), ControlGenerator.GetIcon(g.Key.ToString(), _log))));
        }

        private static StackPanel GetPanelFromTreeItem(ITreeItem i)
        {
            StackPanel panel = new() { Orientation = Orientation.Horizontal, Spacing = 3 };
            panel.Children.Add(new Image { Source = i.Icon });
            panel.Children.Add(new TextBlock { Text = i.Text });
            return panel;
        }

        public abstract void ItemList_ItemDoubleClicked(object sender, TappedEventArgs args);
    }
}
