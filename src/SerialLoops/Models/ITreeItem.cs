using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace SerialLoops.Models
{
    public interface ITreeItem
    {
        public string Text { get; set; }
        public Avalonia.Svg.Svg Icon { get; set; }
        public List<ITreeItem> Children { get; set; }
        public Control GetDisplay();
    }
}
