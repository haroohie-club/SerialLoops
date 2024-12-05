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
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class SearchDialogViewModel : ViewModelBase
{
    public int MinWidth => 550;
    public int MinHeight => 600;
    public int Width { get; set; } = 650;
    public int Height { get; set; } = 420;
    public ICommand OpenItemCommand { get; private set; }
    public ICommand SearchCommand { get; private set; }
    public ICommand CloseCommand { get; private set; }
    public string ResultsCount { get => string.Format(Strings._0__results_found, Items.Count); }
    [Reactive]
    public KeyGesture CloseKeyGesture { get; private set;  }
    [Reactive]
    public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }


    private ILogger _log;
    private Project _project;
    private EditorTabsPanelViewModel _tabs;
    private SearchDialog _searchDialog;

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


    public void Initialize(SearchDialog searchDialog, Project project, EditorTabsPanelViewModel tabs, ILogger log)
    {
        _searchDialog = searchDialog;
        _project = project;
        _tabs = tabs;
        _log = log;

        SearchCommand = ReactiveCommand.Create<string>(Search);
        ReactiveCommand.Create<TextBox>(box => box.Focus());
        OpenItemCommand = ReactiveCommand.Create<TreeDataGrid>(OpenItem);
        CloseCommand = ReactiveCommand.Create(_searchDialog.Close);
        CloseKeyGesture = new KeyGesture(Key.Escape);
    }

    public void Search(string query)
    {
        //todo search
        if (string.IsNullOrWhiteSpace(query))
        {
            Items.Clear();
        }
        Items = new(_project.Items.Where(i => i.DisplayName.Contains(query, System.StringComparison.OrdinalIgnoreCase)));
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

}
