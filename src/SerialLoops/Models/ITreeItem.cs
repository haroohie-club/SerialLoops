using System.Collections.ObjectModel;
using Avalonia.Controls;

namespace SerialLoops.Models
{
    public interface ITreeItem
    {
        public string Text { get; set; }
        public Avalonia.Svg.Svg Icon { get; set; }
        public ObservableCollection<ITreeItem> Children { get; set; }
        public bool IsExpanded { get; set; }
        public Control GetDisplay();
    }
}
