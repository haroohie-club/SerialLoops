using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;
using Tabalonia;

namespace SerialLoops.ViewModels.Dialogs;

public class SearchDialogViewModel : ViewModelBase
{
    public int MinWidth => 850;
    public int MinHeight => 700;
    public int Width { get; set; } = 900;
    public int Height { get; set; } = 750;
    public ICommand OpenItemCommand { get; private set; }
    public ICommand SearchCommand { get; private set; }
    public ICommand DeepSearchCommand { get; private set; }
    public ICommand CloseCommand { get; private set; }

    [Reactive]
    public string SearchStatusLabel { get; private set; } = Strings.Search_Project;
    [Reactive]
    public KeyGesture CloseHotKey { get; private set;  }
    [Reactive]
    public KeyGesture DeepSearchHotKey { get; private set;  }
    [Reactive]
    public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }

    private ILogger _log;
    private Project _project;
    private EditorTabsPanelViewModel _tabs;

    [Reactive]
    public string SearchText { get; set; }

    private List<CheckBox> _itemFilterCheckBoxes = [];
    private HashSet<SearchQuery.DataHolder> _checkedSearchScopes = [SearchQuery.DataHolder.Title];
    private HashSet<ItemDescription.ItemType> _checkedItemScopes = Enum.GetValues<ItemDescription.ItemType>().ToHashSet();

    private ObservableCollection<ItemDescription> _items = new();
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
                        new TemplateColumn<ITreeItem>(
                            null,
                            new FuncDataTemplate<ITreeItem>((val, _) => val?.GetDisplay()),
                            cellEditingTemplate: null, options: null
                        ), i => i.Children
                    )
                }
            };

            Source.ExpandAll();
        }
    }

    private ObservableCollection<ITreeItem> GetSections()
    {
        return new(Items.GroupBy(i => i.Type)
            .OrderBy(g => ControlGenerator.LocalizeItemTypes(g.Key))
            .Select(g => new SectionTreeItem(
                ControlGenerator.LocalizeItemTypes(g.Key),
                g.Select(i => new ItemDescriptionTreeItem(i)),
                ControlGenerator.GetVectorIcon(g.Key.ToString(), _log, size: 16)
            )));
    }

    private SearchQuery GetQuery(string query)
    {
        return new()
        {
            Term = query,
            Scopes = _checkedSearchScopes,
            Types = _checkedItemScopes,
        };
    }


    public SearchDialogViewModel(Project project, EditorTabsPanelViewModel tabs, ILogger log)
    {
        _project = project;
        _tabs = tabs;
        _log = log;

        SearchCommand = ReactiveCommand.CreateFromTask<SearchDialog>(Search);
        DeepSearchCommand = ReactiveCommand.CreateFromTask<SearchDialog>(DeepSearch);
        OpenItemCommand = ReactiveCommand.Create<TreeDataGrid>(OpenItem);
        CloseCommand = ReactiveCommand.Create<SearchDialog>(dialog => dialog.Close());
        CloseHotKey = new(Key.Escape);
        DeepSearchHotKey = new(Key.Enter);

        PopulateSearchScopeFilters();
        PopulateSearchItemFilters();
    }

    public async Task DeepSearch(SearchDialog dialog) => await Search(dialog, true);
    public async Task Search(SearchDialog dialog) => await Search(dialog, false);

    public async Task Search(SearchDialog dialog, bool force)
    {
        SearchQuery query = GetQuery(SearchText ?? string.Empty);

        switch (query.QuickSearch)
        {
            case false when !force:
                SearchStatusLabel = Strings.Press_ENTER_to_execute_search;
                return;
            case true when string.IsNullOrWhiteSpace(SearchText):
            {
                SearchStatusLabel = Strings.Search_Project;
                Items = new();
                break;
            }
            case true:
            {
                var results = _project.GetSearchResults(query, _log);
                Items = new (results);
                SearchStatusLabel = string.Format(Strings._0__results_found, _items.Count);
                break;
            }
            default:
            {
                SearchStatusLabel = Strings.Press_ENTER_to_execute_search;
                if (query.Scopes.Count is 0 || query.Types.Count is 0)
                {
                    await dialog.ShowMessageBoxAsync(Strings.Please_select_at_least_one_search_scope_and_item_filter_, Strings.Invalid_search_terms, ButtonEnum.Ok, Icon.Error, _log);
                    return;
                }
                LoopyProgressTracker tracker = new(Strings.Searching);
                List<ItemDescription> results = [];
                await new ProgressDialog(() => results = _project.GetSearchResults(query, _log, tracker),
                    () =>
                    {
                        Items = new(results);
                        SearchStatusLabel = string.Format(Strings._0__results_found, _items.Count);
                    }, tracker, string.Format(Strings.Searching__0____, _project.Name)).ShowDialog(dialog);;
                break;
            }
        }
    }

    public void OpenItem(TreeDataGrid viewer)
    {
        ItemDescription item = _project.FindItem(((ITreeItem)viewer.RowSelection?.SelectedItem)?.Text);
        if (item is null)
        {
            return;
        }
        _tabs.OpenTab(item);
    }

    private void PopulateSearchScopeFilters()
    {
        int col = 0;
        int row = 0;
        foreach (SearchQuery.DataHolder scope in Enum.GetValues<SearchQuery.DataHolder>())
        {
            var label = new TextBlock {
                Text = ControlGenerator.LocalizeSearchScopes(scope),
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
            label.SetValue(Grid.ColumnProperty, col);
            label.SetValue(Grid.RowProperty, row);

            var box = new CheckBox {
                IsChecked = _checkedSearchScopes.Contains(scope),
                Margin = new(10, 0)
            };
            box.SetValue(Grid.ColumnProperty, col + 1);
            box.SetValue(Grid.RowProperty, row);
            box.IsCheckedChanged += (_, _) =>
            {
                if (_checkedSearchScopes.Contains(scope))
                {
                    _checkedSearchScopes.Remove(scope);
                    return;
                }
                _checkedSearchScopes.Add(scope);
            };

            // _searchDialog.ScopeFiltersGrid.Children.Add(label);
            // _searchDialog.ScopeFiltersGrid.Children.Add(box);

            row++;
            if (row > 6)
            {
                row = 0;
                col += 2;
            }
        }
    }

    private void PopulateSearchItemFilters()
    {
        int col = 0;
        int row = 0;
        foreach (ItemDescription.ItemType type in Enum.GetValues<ItemDescription.ItemType>())
        {
            var label = ControlGenerator.GetControlWithIcon(
                new TextBlock {
                    Text = ControlGenerator.LocalizeItemTypes(type),
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center
                },
                type.ToString(), _log
            );
            label.SetValue(Grid.ColumnProperty, col);
            label.SetValue(Grid.RowProperty, row);

            var box = new CheckBox {
                IsChecked = _checkedItemScopes.Contains(type),
                Margin = new(10, 0)
            };
            box.SetValue(Grid.ColumnProperty, col + 1);
            box.SetValue(Grid.RowProperty, row);
            box.IsCheckedChanged += (_, _) =>
            {
                if (_checkedItemScopes.Contains(type))
                {
                    _checkedItemScopes.Remove(type);
                    return;
                }
                _checkedItemScopes.Add(type);
            };
            _itemFilterCheckBoxes.Add(box);

            // _searchDialog.TypeFiltersGrid.Children.Add(label);
            // _searchDialog.TypeFiltersGrid.Children.Add(box);

            // Span across 3 cols
            row++;
            if (row > 6)
            {
                row = 0;
                col += 2;
            }
        }

        LinkButton toggleButton = new() { Text = Strings.All_Off, FontSize = 16 };
        toggleButton.Command = new SimpleActionCommand(() =>
        {
            bool allOn = _checkedItemScopes.Count == 0;
            _itemFilterCheckBoxes.ForEach(cb => cb.IsChecked = allOn);
            _checkedItemScopes = allOn ? Enum.GetValues<ItemDescription.ItemType>().ToHashSet() : [];
            toggleButton.Text = allOn ? Strings.All_Off : Strings.All_On;
        });
        StackPanel toggleStack = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new(0, 5),
            Children = { toggleButton }
        };
        toggleStack.SetValue(Grid.ColumnProperty, col);
        toggleStack.SetValue(Grid.RowProperty, row);
        toggleStack.SetValue(Grid.ColumnSpanProperty, 2);

        // _searchDialog.TypeFiltersGrid.Children.Add(toggleStack);
    }
}
