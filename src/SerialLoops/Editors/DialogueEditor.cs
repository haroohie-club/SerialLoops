using Eto.Forms;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Editors
{
    internal class DialogueEditor : Editor
    {
        private DialogueItem _item;

        public DialogueEditor(DialogueItem item) : base(item)
        {
            _item = item;
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
