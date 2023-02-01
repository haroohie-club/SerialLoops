using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public class BackgroundEditor : Editor
    {
        public BackgroundEditor(BackgroundItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new ImageView() { Image = new SKGuiImage(((BackgroundItem)Description).GetBackground()) },
                }
            };
        }
    }
}
