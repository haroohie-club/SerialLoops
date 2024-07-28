using Avalonia.Media.Imaging;
using SerialLoops.Lib.Items;
using System.Collections.Generic;

namespace SerialLoops.Models
{
    public class ItemDescriptionTreeItem(ItemDescription description) : ITreeItem
    {
        public string Text { get; set; } = description.DisplayName;
        public Bitmap Icon { get; set; } = null;
        public List<ITreeItem> Children { get; set; } = null;
    }
}
