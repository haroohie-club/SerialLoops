using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;

namespace SerialLoops.ViewModels.Panels;

public class ItemExplorerPanelViewModel : ViewModelBase
{
    private Project _project;
    private EditorTabsPanelViewModel _tabs;
    private ILogger _log;
    private MainWindowViewModel _window;

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
                            return val?.GetDisplay();
                        }), cellEditingTemplate: new FuncDataTemplate<ITreeItem>((val, namescope) =>
                        {
                            if (val is ItemDescriptionTreeItem item)
                            {
                                return item.GetEditableDisplay();
                            }

                            return val?.GetDisplay();
                        }), options: new() { BeginEditGestures = BeginEditGestures.F2 }),
                        i => i.Children
                    ),
                },
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

    [Reactive]
    public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }
    [Reactive]
    public bool ExpandItems { get; set; }

    public ICommand SearchCommand { get; set; }
    public ICommand SearchProjectCommand { get; set; }
    public ICommand OpenItemCommand { get; set; }

    public ItemExplorerPanelViewModel(ICommand searchProjectCommand, MainWindowViewModel window)
    {
        _project = window.OpenProject;
        _tabs = window.EditorTabs;
        _log = window.Log;
        _window = window;

        Items = new(_project.Items);
        SearchProjectCommand = searchProjectCommand;
        SearchCommand = ReactiveCommand.Create<string>(Search);
        OpenItemCommand = ReactiveCommand.Create<TreeDataGrid>(OpenItem);
    }

    public void OpenItem(TreeDataGrid viewer)
    {
        ItemDescription item = _project.FindItem(((ITreeItem)viewer.RowSelection?.SelectedItem)?.Text);
        if (item is not null)
        {
            _tabs.OpenTab(item);
        }
    }

    private ObservableCollection<ITreeItem> GetSections()
    {
        return new(Items
            .Where(i => i.Type != ItemDescription.ItemType.Save)
            .GroupBy(i => i.Type)
            .OrderBy(g => ControlGenerator.LocalizeItemTypes(g.Key))
            .Select(g => new SectionTreeItem(
                ControlGenerator.LocalizeItemTypes(g.Key),
                g.Select(i => new ItemDescriptionTreeItem(i, _window)),
                ControlGenerator.GetVectorIcon(g.Key.ToString(), _log, size: 16)
            )));
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
