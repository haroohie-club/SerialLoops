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
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class SearchDialogViewModel : ViewModelBase
{
    public ICommand OpenItemCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand DeepSearchCommand { get; }
    public ICommand ToggleItemScopesCommand { get; }
    public ICommand CloseCommand { get; }

    [Reactive]
    public string SearchStatusLabel { get; private set; } = Strings.Search_Project;
    [Reactive]
    public KeyGesture CloseHotKey { get; private set; }
    [Reactive]
    public KeyGesture DeepSearchHotKey { get; private set; }
    [Reactive]
    public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }

    private readonly ILogger _log;
    private readonly Project _project;
    private readonly EditorTabsPanelViewModel _tabs;

    [Reactive]
    public string SearchText { get; set; }

    [Reactive]
    public string ToggleText { get; set; }

    private bool _toggleScopesTo = false;

    public ObservableCollection<LocalizedSearchScope> SearchScopes { get; } =
        new(Enum.GetValues<SearchQuery.DataHolder>().Select(h => new LocalizedSearchScope(h)));
    public ObservableCollection<LocalizedItemScope> ItemScopes { get; } =
        new(Enum.GetValues<ItemDescription.ItemType>().Where(i => i != ItemDescription.ItemType.Save).Select(i => new LocalizedItemScope(i)));

    private ObservableCollection<ItemDescription> _items = [];
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
                    ),
                },
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
            Scopes = SearchScopes.Where(s => s.IsActive).Select(s => s.Scope).ToHashSet(),
            Types = ItemScopes.Where(i => i.IsActive).Select(i => i.Type).ToHashSet(),
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
        ToggleItemScopesCommand = ReactiveCommand.Create(() =>
        {
            foreach (LocalizedItemScope scope in ItemScopes)
            {
                scope.IsActive = _toggleScopesTo;
            }
            _toggleScopesTo = !_toggleScopesTo;
            ToggleText = _toggleScopesTo ? Strings.All_On : Strings.All_Off;
        });
        CloseHotKey = new(Key.Escape);
        DeepSearchHotKey = new(Key.Enter);

        SearchScopes[0].IsActive = true;
        ToggleText = Strings.All_Off;
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
                Items = [];
                break;
            }
            case true:
            {
                var results = _project.GetSearchResults(query, _log);
                Items = new(results);
                SearchStatusLabel = string.Format(Strings._0__results_found, _items.Count);
                break;
            }
            default:
            {
                SearchStatusLabel = Strings.Press_ENTER_to_execute_search;
                if (query.Scopes.Count is 0 || query.Types.Count is 0)
                {
                    await dialog.ShowMessageBoxAsync(Strings.Please_select_at_least_one_search_scope_and_item_filter_,
                        Strings.Invalid_search_terms, ButtonEnum.Ok, Icon.Error, _log);
                    return;
                }

                ProgressDialogViewModel tracker = new(string.Format(Strings.Searching__0____, _project.Name));
                List<ItemDescription> results = [];
                tracker.InitializeTasks(() => results = _project.GetSearchResults(query, _log, tracker),
                    () =>
                    {
                        Items = new(results);
                        SearchStatusLabel = string.Format(Strings._0__results_found, _items.Count);
                    });
                await new ProgressDialog { DataContext = tracker }.ShowDialog(dialog);
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
}

public class LocalizedSearchScope(SearchQuery.DataHolder scope) : ReactiveObject
{
    public SearchQuery.DataHolder Scope { get; } = scope;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(scope.ToString());
    [Reactive]
    public bool IsActive { get; set; }
}

public class LocalizedItemScope(ItemDescription.ItemType type) : ReactiveObject
{
    public ItemDescription.ItemType Type { get; } = type;
    public string Icon => $"avares://SerialLoops/Assets/Icons/{Type.ToString()}.svg";
    public string DisplayText { get; } = Strings.ResourceManager.GetString($"{type.ToString()}s");
    [Reactive]
    public bool IsActive { get; set; } = true;
}
