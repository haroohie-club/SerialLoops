using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using SerialLoops.Lib.Script;

namespace SerialLoops.Models;

public class ScriptCommandTreeItem : ITreeItem, IViewFor<ScriptItemCommand>
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

    public ScriptCommandTreeItem(ScriptItemCommand command)
    {
        ViewModel = command;
        this.OneWayBind(ViewModel, vm => vm.Display, v => v._textBlock.Text);
        _panel.Children.Add(_textBlock);
    }

    public Control GetDisplay()
    {
        return _panel;
    }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (ScriptItemCommand)value;
    }

    public ScriptItemCommand ViewModel { get; set; }
}
