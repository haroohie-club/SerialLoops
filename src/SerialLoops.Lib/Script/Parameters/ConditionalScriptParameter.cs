using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class ConditionalScriptParameter : ScriptParameter
    {
        public string Value { get; set; }

        public ConditionalScriptParameter(string name, string value) : base(name, ParameterType.CONDITIONAL)
        {
            Value = value;
        }

        public override ConditionalScriptParameter Clone(Project project, EventFile eventFile)
        {
            var newIndex = eventFile.ConditionalsSection.Objects.Count;
            eventFile.ConditionalsSection.Objects.Add(Value);
            return new(Name, eventFile.ConditionalsSection.Objects[newIndex]);
        }
    }
}
