using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class MapScriptParameter(string name, MapItem map) : ScriptParameter(name, ParameterType.MAP)
{
    public MapItem Map { get; set; } = map;
    public override short[] GetValues(object? obj = null) => [(short)(Map.Map?.Index ?? 0)];

    public override MapScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Map);
    }
}
