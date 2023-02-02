using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Editors
{
    public class CharacterSpriteEditor : Editor
    {
        public CharacterSpriteEditor(CharacterSpriteItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            throw new NotImplementedException();
        }
    }
}
