using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public class EventEditor : Editor
    {
        public EventEditor(EventItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new Label { Text = "Dialogue Editor..todo" }
                }
            };
        }
    }
}
