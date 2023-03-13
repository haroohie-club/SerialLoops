namespace SerialLoops.Lib.Script.Parameters
{
    public class ConditionalScriptParameter : ScriptParameter
    {
        public string Value { get; set; }

        public ConditionalScriptParameter(string name, string value) : base(name, ParameterType.CONDITIONAL)
        {
            Value = value;
        }

        public override ConditionalScriptParameter Clone()
        {
            return new(Name, Value);
        }
    }
}
