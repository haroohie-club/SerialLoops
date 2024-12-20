using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.ViewModels.Editors;

namespace SerialLoops.Models;

public class ScriptSectionTreeItem : ITreeItem, IViewFor<ReactiveScriptSection>
{
    private TextBlock _textBlock = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
    };
    StackPanel _panel = new()
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center,
        Spacing = 3,
        Margin = new(2),
    };

    public string Text { get; set; }
    public Avalonia.Svg.Svg Icon { get; set; } = null;
    public ObservableCollection<ITreeItem> Children { get; set; }
    public bool IsExpanded { get; set; } = true;

    public ScriptSectionTreeItem(ReactiveScriptSection section, List<ScriptItemCommand> commands)
    {
        ViewModel = section;
        this.OneWayBind(ViewModel, vm => vm.Commands, v => v.Children);
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
        set => ViewModel = (ReactiveScriptSection)value;
    }

    public ReactiveScriptSection ViewModel { get; set; }
}
