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
    }
}
