using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class ChibiScriptParameter(string name, ChibiItem chibi) : ScriptParameter(name, ParameterType.CHIBI)
{
    public ChibiItem Chibi { get; set; } = chibi;
    public override short[] GetValues(object? obj = null) => [(short)Chibi.TopScreenIndex];

    public override ChibiScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Chibi);
    }
}
