using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class ShortScriptParameter : ScriptParameter
    {
        public short Value { get; set; }

        public ShortScriptParameter(string name, short value) : base(name, ParameterType.SHORT)
        {
            Value = value;
        }

        public override ShortScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Value);
        }
    }
}
