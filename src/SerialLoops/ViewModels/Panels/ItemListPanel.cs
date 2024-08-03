using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using HaruhiChokuretsuLib.Util;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;

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
        [Reactive]
        public bool ExpandItems { get; set; }

        public void InitializeItems(List<ItemDescription> items, bool expandItems, ILogger log)
        {
            Items = items;
            ExpandItems = expandItems;
            
            _log = log;
        }

        private ObservableCollection<ITreeItem> GetSections()
        {
            return new ObservableCollection<ITreeItem>(Items.GroupBy(i => i.Type).OrderBy(g => LocalizeItemTypes(g.Key))
                .Select(g => new SectionTreeItem(
                    LocalizeItemTypes(g.Key),
                    g.Select(i => new ItemDescriptionTreeItem(i)),
                    ControlGenerator.GetVectorIcon(g.Key.ToString(), _log, size: 16)
                )));
        }

        private static string LocalizeItemTypes(ItemDescription.ItemType type)
        {
            return type switch
            {
                ItemDescription.ItemType.Background => Strings.Backgrounds,
                ItemDescription.ItemType.BGM => Strings.BGMs,
                ItemDescription.ItemType.Character => Strings.Characters,
                ItemDescription.ItemType.Character_Sprite => Strings.Character_Sprites,
                ItemDescription.ItemType.Chess_Puzzle => Strings.Chess_Puzzles,
                ItemDescription.ItemType.Chibi => Strings.Chibis,
                ItemDescription.ItemType.Group_Selection => Strings.Group_Selections,
                ItemDescription.ItemType.Item => Strings.Items,
                ItemDescription.ItemType.Layout => Strings.Layouts,
                ItemDescription.ItemType.Map => Strings.Maps,
                ItemDescription.ItemType.Place => Strings.Places,
                ItemDescription.ItemType.Puzzle => Strings.Puzzles,
                ItemDescription.ItemType.Scenario => Strings.Scenario,
                ItemDescription.ItemType.Script => Strings.Scripts,
                ItemDescription.ItemType.SFX => Strings.SFXs,
                ItemDescription.ItemType.System_Texture => Strings.System_Textures,
                ItemDescription.ItemType.Topic => Strings.Topics,
                ItemDescription.ItemType.Transition => Strings.Transitions,
                ItemDescription.ItemType.Voice => Strings.Voices,
                _ => "UNKNOWN TYPE",
            };
        }

        public void SetupViewer(TreeView viewer)
        {
            viewer.ItemTemplate = new FuncTreeDataTemplate<ITreeItem>((item, namescope) => item.GetDisplay(), item => item.Children);
            viewer.ItemsSource = Source;
            viewer.ItemContainerTheme = new(typeof(TreeViewItem)) { BasedOn = (ControlTheme)Application.Current.FindResource(typeof(TreeViewItem)) };
            viewer.ItemContainerTheme.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, new Binding("IsExpanded")));
            if (ExpandItems)
            {
                foreach (ITreeItem item in viewer.ItemsSource)
                {
                    if (item is SectionTreeItem section)
                    {
                        section.IsExpanded = true;
                    }
                }
            }
            viewer.AddHandler(InputElement.KeyDownEvent, Viewer_KeyDown, RoutingStrategies.Tunnel);
            viewer.DoubleTapped += ItemList_ItemDoubleClicked;
        }

        private void Viewer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ItemList_ItemDoubleClicked(sender, null);
                e.Handled = true;
            }
        }
        public abstract void ItemList_ItemDoubleClicked(object sender, TappedEventArgs args);
    }
}
