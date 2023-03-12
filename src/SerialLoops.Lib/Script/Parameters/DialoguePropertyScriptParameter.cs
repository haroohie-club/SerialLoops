using HaruhiChokuretsuLib.Archive.Data;

namespace SerialLoops.Lib.Script.Parameters
{
    public class DialoguePropertyScriptParameter : ScriptParameter
    {
        public MessageInfo DialogueProperties { get; set; }

        public DialoguePropertyScriptParameter(string name, MessageInfo dialogueProperties) : base(name, ParameterType.DIALOGUE_PROPERTY)
        {
            DialogueProperties = dialogueProperties;
        }

        public override DialoguePropertyScriptParameter Clone()
        {
            return new(Name, DialogueProperties);
        }
    }
}
