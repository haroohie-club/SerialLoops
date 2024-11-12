using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ScreenScriptParameter(string name, short screen) : ScriptParameter(name, ParameterType.SCREEN)
{
    public DsScreen Screen { get; set; } = (DsScreen)screen;
    public override short[] GetValues(object? obj = null) => [(short)Screen];

    public override ScreenScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Screen);
    }

    public enum DsScreen : short
    {
        BOTTOM = 0,
        TOP = 1,
        BOTH = 2,
    }
}
