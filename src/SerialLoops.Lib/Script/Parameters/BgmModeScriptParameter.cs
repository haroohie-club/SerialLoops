using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class BgmModeScriptParameter : ScriptParameter
{
    public BgmMode Mode { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { (short)Mode };

    public override string GetValueString(Project project)
    {
        return project.Localize(Mode.ToString());
    }

    public BgmModeScriptParameter(string name, short mode) : base(name, ParameterType.BGM_MODE)
    {
        Mode = (BgmMode)mode;
    }

    public enum BgmMode : short
    {
        Start = 2,
        Stop = 4,
    }

    public override BgmModeScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Mode);
    }
}
