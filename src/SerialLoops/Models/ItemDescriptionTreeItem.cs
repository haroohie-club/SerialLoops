using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.Models;

public class ItemDescriptionTreeItem : ITreeItem, IViewFor<ItemDescription>
{
    private TextBlock _textBlock = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
    };
    private TextBox _textBox = new();
    StackPanel _panel = new()
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center,
        Spacing = 3,
        Margin = new(2),
    };

    private StackPanel _editablePanel = new()
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center,
        Spacing = 3,
        IsEnabled = true,
    };

    private MainWindowViewModel _window;

    public string Text { get; set; }
    public Avalonia.Svg.Svg Icon { get; set; } = null;
    public ObservableCollection<ITreeItem> Children { get; set; } = null;
    public bool IsExpanded { get; set; } = false;

    public ItemDescriptionTreeItem(ItemDescription item, MainWindowViewModel window = null)
    {
        ViewModel = item;
        _window = window;
        this.OneWayBind(ViewModel, vm => vm.DisplayName, v => v._textBlock.Text);
        this.Bind(ViewModel, vm => vm.DisplayName, v => v.Text);
        _panel.Children.Add(_textBlock);

        if (_window is not null)
        {
            ICommand findReferencesCommand = ReactiveCommand.Create(FindReferences);
            _panel.ContextMenu = new();
            MenuItem references = new() { Header = Strings.ItemsPanelContextFindItems, Command = findReferencesCommand};
            _panel.ContextMenu.Items.Add(references);
        }

        this.Bind(ViewModel, vm => vm.DisplayName, v => v._textBox.Text);
        _textBox.Unloaded += (sender, args) =>
        {
            if (ViewModel is not null)
            {
                _window.ItemExplorer.Source.SortBy(_window.ItemExplorer.Source.Columns[0], ListSortDirection.Ascending);
                _window.OpenProject.ItemNames[ViewModel.Name] = ViewModel.DisplayName;
                _window.OpenProject.Save(_window.Log);
            }
        };
        _editablePanel.Children.Add(_textBox);
    }

    public Control GetDisplay()
    {
        return _panel;
    }

    public Control GetEditableDisplay()
    {
        if (ViewModel?.CanRename ?? false)
        {
            return _editablePanel;
        }
        else
        {
            return _panel;
        }
    }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (ItemDescription)value;
    }

    private void FindReferences()
    {
        if (_window is null)
        {
            return;
        }
        ItemReferencesDialogViewModel referencesViewModel = new(ViewModel, _window.OpenProject, _window.EditorTabs, _window.Log);
        ItemReferencesDialog referencesDialog = new() { DataContext = referencesViewModel };
        referencesDialog.Show(_window.Window);
    }

    public ItemDescription ViewModel { get; set; }
}
