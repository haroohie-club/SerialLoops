using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using HaruhiChokuretsuLib.Archive.Event;
using ReactiveUI;
using SerialLoops.Lib.Script;

namespace SerialLoops.Models;

public class ScriptSectionTreeItem : ITreeItem, IViewFor<ScriptSection>
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
    public ObservableCollection<ITreeItem> Children { get; set; }
    public bool IsExpanded { get; set; } = true;

    public ScriptSectionTreeItem(ScriptSection section, List<ScriptItemCommand> commands)
    {
        ViewModel = section;
        Children = new([.. commands.Select(c => new ScriptCommandTreeItem(c))]);
        this.OneWayBind(ViewModel, vm => vm.Name, v => v._textBlock.Text);
        this.Bind(ViewModel, vm => vm.Name, v => v.Text);
        _panel.Children.Add(_textBlock);
    }

    public Control GetDisplay()
    {
        return _panel;
    }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (ScriptSection)value;
    }

    public ScriptSection ViewModel { get; set; }
}