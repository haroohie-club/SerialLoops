using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script
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
