using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class BgScriptParameter(string name, BackgroundItem background, bool kinetic)
    : ScriptParameter(name, ParameterType.BG)
{
    public BackgroundItem Background { get; set; } = background;
    public bool Kinetic { get; set; } = kinetic;
    public override short[] GetValues(object? obj = null) => [(short)(Background?.Id ?? 0)];

    public override BgScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Background, Kinetic);
    }
}
