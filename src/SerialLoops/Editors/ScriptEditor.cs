using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public class ScriptEditor : Editor
    {
        private ScriptItem _script;

        public ScriptEditor(ScriptItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            _script = (ScriptItem)Description;
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
