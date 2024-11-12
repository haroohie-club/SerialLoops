using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class SfxModeScriptParameter(string name, short mode) : ScriptParameter(name, ParameterType.SFX_MODE)
{
    public SfxMode Mode { get; set; } = (SfxMode)mode;
    public override short[] GetValues(object? obj = null) => [(short)Mode];

    public override SfxModeScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Mode);
    }

    public enum SfxMode : short
    {
        START = 6,
        STOP = 7,
    }
}
