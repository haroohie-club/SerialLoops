using Eto.Forms;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public class EventEditor : Editor
    {
        private EventItem _event;

        public EventEditor(EventItem item) : base(item)
        {
            _event = item;
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
