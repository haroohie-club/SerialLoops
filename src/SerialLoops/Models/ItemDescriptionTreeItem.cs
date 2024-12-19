using System.Collections.ObjectModel;
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
    private TextBlock _textBlock = new();
    StackPanel _panel = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 3,
        Margin = new(2),
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
            MenuItem references = new() { Header = Strings.Find_References___, Command = findReferencesCommand};
            _panel.ContextMenu.Items.Add(references);
        }
    }

    public Control GetDisplay()
    {
        return _panel;
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
