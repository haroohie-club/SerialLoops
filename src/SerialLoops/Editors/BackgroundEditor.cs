using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Utility;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public class BackgroundEditor : Editor
    {
        private BackgroundItem _bg;

        public BackgroundEditor(BackgroundItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            _bg = (BackgroundItem)Description;
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new ImageView() { Image = new SKGuiImage(_bg.GetBackground()) },
                }
            };
        }
    }
}
