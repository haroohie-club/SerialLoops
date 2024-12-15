using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
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
    public KeyGesture CloseKeyGesture { get; private set;  }
    [Reactive]
    public KeyGesture DeepSearchGesture { get; private set;  }
    [Reactive]
    public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }

    private ILogger _log;
    private Project _project;
    private EditorTabsPanelViewModel _tabs;
    private SearchDialog _searchDialog;

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
        return new ObservableCollection<ITreeItem>(Items.GroupBy(i => i.Type)
            .OrderBy(g => ControlGenerator.LocalizeItemTypes(g.Key))
            .Select(g => new SectionTreeItem(
                ControlGenerator.LocalizeItemTypes(g.Key),
                g.Select(i => new ItemDescriptionTreeItem(i)),
                ControlGenerator.GetVectorIcon(g.Key.ToString(), _log, size: 16)
            )));
    }

    private SearchQuery GetQuery(string query)
    {
        return new SearchQuery
        {
            Term = query,
            Scopes = _checkedSearchScopes,
            Types = _checkedItemScopes,
        };
    }


    public void Initialize(SearchDialog searchDialog, Project project, EditorTabsPanelViewModel tabs, ILogger log)
    {
        _searchDialog = searchDialog;
        _project = project;
        _tabs = tabs;
        _log = log;

        SearchCommand = ReactiveCommand.Create<string>(Search);
        DeepSearchCommand = ReactiveCommand.Create(() => Search(_searchDialog.Search.Text, true));
        ReactiveCommand.Create<TextBox>(box => box.Focus());
        OpenItemCommand = ReactiveCommand.Create<TreeDataGrid>(OpenItem);
        CloseCommand = ReactiveCommand.Create(_searchDialog.Close);
        CloseKeyGesture = new KeyGesture(Key.Escape);
        DeepSearchGesture = new KeyGesture(Key.Enter);

        PopulateSearchScopeFilters();
        PopulateSearchItemFilters();
    }

    public void DeepSearch(string text) => Search(text, true);
    public void Search(string text) => Search(text, false);

    public async void Search(string text, bool force)
    {
        SearchQuery query = GetQuery(text ?? string.Empty);

        switch (query.QuickSearch)
        {
            case false when !force:
                SearchStatusLabel = Strings.Press_ENTER_to_execute_search;
                return;
            case true when string.IsNullOrWhiteSpace(text):
            {
                SearchStatusLabel = Strings.Search_Project;
                Items = new ObservableCollection<ItemDescription>();
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
                    await _searchDialog.ShowMessageBoxAsync(Strings.Please_select_at_least_one_search_scope_and_item_filter_, Strings.Invalid_search_terms, ButtonEnum.Ok, Icon.Error, _log);
                    return;
                }
                LoopyProgressTracker tracker = new(Strings.Searching);
                List<ItemDescription> results = [];
                await new ProgressDialog(() => results = _project.GetSearchResults(query, _log, tracker),
                    () =>
                    {
                        Items = new(results);
                        SearchStatusLabel = string.Format(Strings._0__results_found, _items.Count);
                    }, tracker, string.Format(Strings.Searching__0____, _project.Name)).ShowDialog(_searchDialog);;
                break;
            }
        }
    }

    public void OpenItem(TreeDataGrid viewer)
    {
        ItemDescription item = _project.FindItem(((ITreeItem)viewer.RowSelection.SelectedItem)?.Text);
        if (item is null)
        {
            return;
        }

        _searchDialog.Close();
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
                Margin = new Thickness(10, 0)
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

            _searchDialog.ScopeFiltersGrid.Children.Add(label);
            _searchDialog.ScopeFiltersGrid.Children.Add(box);

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
                Margin = new Thickness(10, 0)
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

            _searchDialog.TypeFiltersGrid.Children.Add(label);
            _searchDialog.TypeFiltersGrid.Children.Add(box);

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
            Margin = new Thickness(0, 5),
            Children = { toggleButton }
        };
        toggleStack.SetValue(Grid.ColumnProperty, col);
        toggleStack.SetValue(Grid.RowProperty, row);
        toggleStack.SetValue(Grid.ColumnSpanProperty, 2);

        _searchDialog.TypeFiltersGrid.Children.Add(toggleStack);
    }

}
