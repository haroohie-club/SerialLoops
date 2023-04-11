using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class DialoguePropertyScriptParameter : ScriptParameter
    {
        public MessageInfo DialogueProperties { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)((MessageInfoFile)obj).MessageInfos.FindIndex(m => m.Character == DialogueProperties.Character) };

        public DialoguePropertyScriptParameter(string name, MessageInfo dialogueProperties) : base(name, ParameterType.DIALOGUE_PROPERTY)
        {
            DialogueProperties = dialogueProperties;
        }

        public override DialoguePropertyScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, DialogueProperties);
        }
    }
}
