using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class SfxScriptParameter(string name, SfxItem sfx) : ScriptParameter(name, ParameterType.SFX)
{
    public SfxItem Sfx { get; set; } = sfx;
    public override short[] GetValues(object? obj = null) => [Sfx.Index];

    public override SfxScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Sfx);
    }
}
