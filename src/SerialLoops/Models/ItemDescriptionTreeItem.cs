using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using SerialLoops.Lib.Items;

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

    public string Text { get; set; }
    public Avalonia.Svg.Svg Icon { get; set; } = null;
    public ObservableCollection<ITreeItem> Children { get; set; } = null;
    public bool IsExpanded { get; set; } = false;

    public ItemDescriptionTreeItem(ItemDescription item)
    {
        ViewModel = item;
        this.OneWayBind(ViewModel, vm => vm.DisplayName, v => v._textBlock.Text);
        this.Bind(ViewModel, vm => vm.DisplayName, v => v.Text);
        _panel.Children.Add(_textBlock);
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

    public ItemDescription ViewModel { get; set; }
}