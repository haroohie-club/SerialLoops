using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class BoolScriptParameter(string name, bool value, short trueValue = 1, short falseValue = 0) : ScriptParameter(name, ParameterType.BOOL)
    {
        public bool Value { get; set; } = value;
        public short TrueValue { get; set; } = trueValue;
        public short FalseValue { get; set; } = falseValue;
        public override short[] GetValues(object obj = null) => [Value ? TrueValue : FalseValue];

        public override BoolScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Value);
        }
    }
}
