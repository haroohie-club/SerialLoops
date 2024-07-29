using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace SerialLoops.ViewModels.Panels
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
                Source = new ObservableCollection<ITreeItem>(GetSections());
            }
        }
        public ObservableCollection<ITreeItem> Source { get; set; }

        protected ILogger _log;
        protected bool ExpandItems { get; set; }

        public void InitializeItems(List<ItemDescription> items, bool expandItems, ILogger log)
        {
            Items = items;
            ExpandItems = expandItems;
            _log = log;
        }

        private ObservableCollection<ITreeItem> GetSections()
        {
            return new ObservableCollection<ITreeItem>(Items.GroupBy(i => i.Type).OrderBy(g => g.Key)
                .Select(g => new SectionTreeItem($"{g.Key}s", g.Select(i => new ItemDescriptionTreeItem(i)), ControlGenerator.GetIcon(g.Key.ToString(), _log))));
        }

        public void SetupViewer(TreeView viewer)
        {
            viewer.ItemTemplate = new FuncTreeDataTemplate<ITreeItem>((item, namescope) => item.GetDisplay(), item => item.Children);
            viewer.ItemsSource = Source;
            viewer.DoubleTapped += ItemList_ItemDoubleClicked;
        }

        public abstract void ItemList_ItemDoubleClicked(object sender, TappedEventArgs args);
    }
}
