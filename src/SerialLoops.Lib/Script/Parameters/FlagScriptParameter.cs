using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class FlagScriptParameter : ScriptParameter
    {
        private short _internalFlagId;

        public short Id { get => (short)(_internalFlagId - 1); set => _internalFlagId = (short)(value + 1); }
        public string FlagName => Id > 100 && Id < 121 ? $"G{Id - 100:D2}" : $"F{Id:D2}";
        public override short[] GetValues(object obj = null) => new short[] { Id };

        public FlagScriptParameter(string name, short id) : base(name, ParameterType.FLAG)
        {
            _internalFlagId = id;
        }

        public override FlagScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Id);
        }
    }
}
