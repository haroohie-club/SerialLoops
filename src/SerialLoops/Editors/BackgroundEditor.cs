using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class BackgroundEditor : Editor
    {
        private BackgroundItem _bg;

        public BackgroundEditor(BackgroundItem item, ILogger log, IProgressTracker tracker) : base(item, log, tracker)
        {
        }

        public override Container GetEditorPanel()
        {
            _bg = (BackgroundItem)Description;
            StackLayout extrasInfo = new();
            if (!string.IsNullOrEmpty(_bg.CgName))
            {
                extrasInfo.Items.Add(_bg.CgName);
                extrasInfo.Items.Add($"Unknown Extras Short: {_bg.ExtrasShort}");
                extrasInfo.Items.Add($"Unknown Extras Integer: {_bg.ExtrasInt}");
            }
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new ImageView() { Image = new SKGuiImage(_bg.GetBackground()) },
                    $"{_bg.Id} (0x{_bg.Id:X3})",
                    extrasInfo,
                }
            };
        }
    }
}
