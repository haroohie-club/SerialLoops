using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ScreenScriptParameter : ScriptParameter
{
    public DsScreen Screen { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { (short)Screen };

    public ScreenScriptParameter(string name, short screen) : base(name, ParameterType.SCREEN)
    {
        Screen = (DsScreen)screen;
    }

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