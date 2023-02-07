using Eto.Forms;
using SerialLoops.Lib.Script;

namespace SerialLoops.Controls
{
    public class CommandDropDown : DropDown
    {
        public ScriptItemCommand Command { get; set; }
        public int ParameterIndex { get; set; }
    }
}
