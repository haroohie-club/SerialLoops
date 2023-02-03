using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Utility;
using SerialLoops.Lib.Items;
using System.Linq;

namespace SerialLoops.Editors
{
    public class BackgroundEditor : Editor
    {
        private BackgroundItem _bg;

        public BackgroundEditor(BackgroundItem item, ILogger log) : base(item, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _bg = (BackgroundItem)Description;
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new ImageView() { Image = new SKGuiImage(_bg.GetBackground()) },
                    $"Used in the following scripts: {string.Join(", ", _bg.ScriptUses.Select(s => s.ScriptName))}",
                }
            };
        }
    }
}
