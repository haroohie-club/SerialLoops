using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Editors
{
    public class BackgroundEditor : Editor
    {
        public BackgroundEditor(BackgroundItem item) : base(item)
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
