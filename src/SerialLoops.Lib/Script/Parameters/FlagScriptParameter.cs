using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class FlagScriptParameter(string name, short id) : ScriptParameter(name, ParameterType.FLAG)
{
    private short _internalFlagId = id;

    public short Id { get => (short)(_internalFlagId - 1); set => _internalFlagId = (short)(value + 1); }
    public string FlagName => Id > 100 && Id < 121 ? $"G{Id - 100:D2}" : $"F{Id:D2}";
    public override short[] GetValues(object? obj = null) => [Id];

    public override FlagScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Id);
    }
}
