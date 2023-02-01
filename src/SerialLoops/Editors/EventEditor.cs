using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public class ScriptEditor : Editor
    {
        public ScriptEditor(ScriptItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new Label { Text = "Script Editor..todo" }
                }
            };
        }
    }
}
