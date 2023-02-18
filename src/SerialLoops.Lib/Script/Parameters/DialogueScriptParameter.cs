using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using System.Linq;

namespace SerialLoops.Lib.Script.Parameters
{
    public class DialogueScriptParameter : ScriptParameter
    {
        public DialogueLine Line { get; set; }

        public DialogueScriptParameter(string name, DialogueLine line) : base(name, ParameterType.DIALOGUE)
        {
            Line = line;
        }
    }
}
