using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Models;

public class SectionTreeItem(string sectionName, IEnumerable<ITreeItem> children, Avalonia.Svg.Svg icon) : ReactiveObject, ITreeItem
{
    public string Text { get; set; } = sectionName;
    public Avalonia.Svg.Svg Icon { get; set; } = icon;
    public ObservableCollection<ITreeItem> Children { get; set; } = new(children);
    [Reactive]
    public bool IsExpanded { get; set; }

    public Control GetDisplay()
    {
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
        };
        if (Icon is not null)
        {
            if (Icon.Parent is not null)
            {
                ((StackPanel)Icon.Parent).Children.Clear();
            }
            panel.Children.Add(Icon);
        }
        panel.Children.Add(new TextBlock { Text = Text });
        return panel;
    }
}