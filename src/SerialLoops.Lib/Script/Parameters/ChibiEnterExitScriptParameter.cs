using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ChibiEnterExitScriptParameter : ScriptParameter
{
    public ChibiEnterExitType Mode { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { (short)Mode };

    public ChibiEnterExitScriptParameter(string name, short mode) : base(name, ParameterType.CHIBI_ENTER_EXIT)
    {
        Mode = (ChibiEnterExitType)mode;
    }

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