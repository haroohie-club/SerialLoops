using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ShortScriptParameter(string name, short value) : ScriptParameter(name, ParameterType.SHORT)
{
    public short Value { get; set; } = value;
    public override short[] GetValues(object? obj = null) => [Value];

    public override ShortScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Value);
    }
}
