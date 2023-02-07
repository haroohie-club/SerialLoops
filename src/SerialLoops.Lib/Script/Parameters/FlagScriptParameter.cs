namespace SerialLoops.Lib.Script.Parameters
{
    public class FlagScriptParameter : ScriptParameter
    {
        private short _internalFlagId;

        public bool Global { get; set; }
        public short Id => (short)(_internalFlagId - 1);
        public string FlagName => Global ? $"G{Id:D2}" : $"F{Id:D2}";

        public FlagScriptParameter(string name, short id, bool global) : base(name, ParameterType.FLAG)
        {
            _internalFlagId = id;
            Global = global;
        }
    }
}
