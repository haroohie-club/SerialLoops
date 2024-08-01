using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SerialLoops.Models
{
    public class SectionTreeItem(string sectionName, IEnumerable<ITreeItem> children, Avalonia.Svg.Svg icon) : ObservableObject, ITreeItem
    {
        private bool _isExpanded;

        public string Text { get; set; } = sectionName;
        public Avalonia.Svg.Svg Icon { get; set; } = icon;
        public List<ITreeItem> Children { get; set; } = children.ToList();
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public Control GetDisplay()
        {
            StackPanel panel = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                Margin = new(2),
            };
            if (Icon is not null)
            {
                panel.Children.Add(Icon); // We don't add a control if there's no icon (unlike bitmaps)
            }
            panel.Children.Add(new TextBlock { Text = Text });
            return panel;
        }
    }
}
