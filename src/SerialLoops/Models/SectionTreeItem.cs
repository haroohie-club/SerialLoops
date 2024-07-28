using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Models
{
    public class SectionTreeItem(string sectionName, IEnumerable<ITreeItem> children, Bitmap icon) : ITreeItem
    {
        public string Text { get; set; } = sectionName;
        public Bitmap Icon { get; set; } = icon;
        public List<ITreeItem> Children { get; set; } = children.ToList();

        public Control GetDisplay()
        {
            StackPanel panel = new()
            {
                Spacing = 3,
                Margin = new(2),
            };
            panel.Children.Add(new Image { Source = Icon });
            panel.Children.Add(new TextBlock { Text = Text });
            return panel;
        }
    }
}
