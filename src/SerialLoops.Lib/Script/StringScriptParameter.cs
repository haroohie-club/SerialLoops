namespace SerialLoops.Lib.Script
{
    public class StringScriptParameter : ScriptParameter
    {
        public StringParameterType StringType { get; set; }
        public string Value { get; set; }

        public StringScriptParameter(string name, StringParameterType type, string value) : base(name, ParameterType.STRING)
        {
            StringType = type;
            Value = value;
        }

        public enum StringParameterType
        {
            CONDITIONAL,
            DIALOGUE,
            OPTION,
        }
    }
}
