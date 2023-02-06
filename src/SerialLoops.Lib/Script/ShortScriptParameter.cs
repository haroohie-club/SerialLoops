namespace SerialLoops.Lib.Script
{
    public class ShortScriptParameter : ScriptParameter
    {
        public short Value { get; set; }

        public ShortScriptParameter(string name, short value) : base(name, ParameterType.SHORT)
        {
            Value = value;
        }
    }
}
