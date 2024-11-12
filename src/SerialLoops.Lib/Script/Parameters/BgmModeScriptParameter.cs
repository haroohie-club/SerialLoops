using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class BgmModeScriptParameter(string name, short mode) : ScriptParameter(name, ParameterType.BGM_MODE)
{
    public BgmMode Mode { get; set; } = (BgmMode)mode;
    public override short[] GetValues(object? obj = null) => [(short)Mode];

    public enum BgmMode : short
    {
        START = 2,
        STOP = 4,
    }

    public override BgmModeScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Mode);
    }
}
