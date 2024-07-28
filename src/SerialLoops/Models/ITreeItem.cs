using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace SerialLoops.Models
{
    public interface ITreeItem
    {
        public string Text { get; set; }
        public Bitmap Icon { get; set; }
        public List<ITreeItem> Children { get; set; }
    }
}
