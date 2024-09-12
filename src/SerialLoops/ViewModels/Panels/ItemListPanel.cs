using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        private ObservableCollection<ItemDescription> _items;
        public ObservableCollection<ItemDescription> Items
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
                            new TemplateColumn<ITreeItem>("Section", new FuncDataTemplate<ITreeItem>((val, namescope) =>
                            {
                                StackPanel panel = new()
                                {
                                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                                    Spacing = 3,
                                };
                                panel.Children.Add(val.Icon);
                                panel.Children.Add(new TextBlock { Text = val.Text });
                                return panel;
                            }
                            )),
                            val => val.Children
                        )
                    }
                };
                if (ExpandItems)
                {
                    foreach (ITreeItem item in Source.Items)
                    {
                        item.IsExpanded = true;
                    }
                }
            }
        }

        protected ILogger _log;

        [Reactive]
        public HierarchicalTreeDataGridSource<ITreeItem> Source { get; set; }
        [Reactive]
        public bool ExpandItems { get; set; }

        public void InitializeItems(List<ItemDescription> items, bool expandItems, ILogger log)
        {
            Items = new(items);
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

        public void SetupViewer(TreeDataGrid viewer)
        {
            viewer.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(Source), BindingMode.TwoWay));
            if (ExpandItems)
            {
                foreach (ITreeItem item in viewer.Source.Items)
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
