using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using SerialLoops.Lib.Items;
using System.Collections.Generic;

namespace SerialLoops.Models
{
    public class ItemDescriptionTreeItem(ItemDescription description) : ITreeItem
    {
        public string Text { get; set; } = description.DisplayName;
        public Avalonia.Svg.Svg Icon { get; set; } = null;
        public List<ITreeItem> Children { get; set; } = null;

        public Control GetDisplay()
        {
            StackPanel panel = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                Margin = new(2),
            };
            panel.Children.Add(new TextBlock { Text = Text });
            return panel;
        }
    }
}
