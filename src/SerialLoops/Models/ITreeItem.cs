using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace SerialLoops.Models
{
    public interface ITreeItem
    {
        public string Text { get; set; }
        public Bitmap Icon { get; set; }
        public List<ITreeItem> Children { get; set; }
        public Control GetDisplay();
    }
}
