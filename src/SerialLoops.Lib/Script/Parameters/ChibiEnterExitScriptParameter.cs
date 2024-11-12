using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ChibiEnterExitScriptParameter(string name, short mode)
    : ScriptParameter(name, ParameterType.CHIBI_ENTER_EXIT)
{
    public ChibiEnterExitType Mode { get; set; } = (ChibiEnterExitType)mode;
    public override short[] GetValues(object? obj = null) => [(short)Mode];

    public override ChibiEnterExitScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Mode);
    }

    public enum ChibiEnterExitType
    {
        ENTER = 0,
        EXIT = 1,
    }
}
