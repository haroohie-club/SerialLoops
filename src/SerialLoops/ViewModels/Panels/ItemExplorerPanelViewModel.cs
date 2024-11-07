using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;

namespace SerialLoops.ViewModels.Panels
{
    public class ItemExplorerPanelViewModel : ViewModelBase
    {
        private Project _project;
        private EditorTabsPanelViewModel _tabs;
        private ILogger _log;

        private ObservableCollection<ItemDescription> _items;
        public ObservableCollection<ItemDescription> Items
        {
            get => _items;
            set
            {
                this.RaiseAndSetIfChanged(ref _items, value);
                Source = new(GetSections())
                {
                    Columns =
                    {
                        new HierarchicalExpanderColumn<ITreeItem>(
                            new TemplateColumn<ITreeItem>(null, new FuncDataTemplate<ITreeItem>((val, namescope) =>
                            {
                                return GetItemPanel(val);
                            }), cellEditingTemplate: new FuncDataTemplate<ITreeItem>((val, namescope) =>
                            {
                                // Eventually we can maybe do rename logic here
                                return GetItemPanel(val);
                            }), options: new() { BeginEditGestures = BeginEditGestures.F2 }),
                            i => i.Children
                        )
                    }
                };

                if (ExpandItems)
                {
                    Source.ExpandAll();
                }
                else
                {
                    Source.CollapseAll();
                }
            }
        }

        private StackPanel GetItemPanel(ITreeItem val)
        {
            if (val is null)
            {
                return null;
            }
            StackPanel panel = new()
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 3,
            };
            if (val.Icon is not null)
            {
                if (val.Icon.Parent is not null)
                {
                    ((StackPanel)val.Icon.Parent).Children.Clear();
                }
                panel.Children.Add(val.Icon);
            }
            panel.Children.Add(new TextBlock { Text = val.Text });
            return panel;
        }

        [Reactive]
        public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }
        [Reactive]
        public bool ExpandItems { get; set; }

        public ICommand SearchCommand { get; set; }
        public ICommand OpenItemCommand { get; set; }

        public ItemExplorerPanelViewModel(Project project, EditorTabsPanelViewModel tabs, ILogger log)
        {
            _project = project;
            _tabs = tabs;
            _log = log;
            Items = new(project.Items);
            SearchCommand = ReactiveCommand.Create<string>(Search);
            OpenItemCommand = ReactiveCommand.Create<TreeDataGrid>(OpenItem);
        }

        public void OpenItem(TreeDataGrid viewer)
        {
            ItemDescription item = _project.FindItem(((ITreeItem)viewer.RowSelection.SelectedItem)?.Text);
            if (item is not null)
            {
                _tabs.OpenTab(item);
            }
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

        private void Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                ExpandItems = false;
                Items = new(_project.Items);
            }
            else
            {
                ExpandItems = true;
                Items = new(_project.Items.Where(i => i.DisplayName.Contains(query, System.StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
