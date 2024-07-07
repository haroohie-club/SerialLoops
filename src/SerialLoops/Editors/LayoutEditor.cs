using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class LayoutEditor(LayoutItem layoutItem, ILogger log) : Editor(layoutItem, log)
    {
        private LayoutItem _layout;

        public override Container GetEditorPanel()
        {
            _layout = (LayoutItem)Description;

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    new ImageView() { Image = new SKGuiImage(_layout.GetLayoutImage()) },
                    new Scrollable
                    {
                        Content = new StackLayout
                        {

                        },
                    },
                }
            };
        }
    }
}
