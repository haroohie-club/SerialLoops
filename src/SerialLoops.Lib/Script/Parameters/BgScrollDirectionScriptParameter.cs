using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class BgScrollDirectionScriptParameter(string name, short scrollDirection)
    : ScriptParameter(name, ParameterType.BG_SCROLL_DIRECTION)
{
    public BgScrollDirection ScrollDirection { get; set; } = (BgScrollDirection)scrollDirection;
    public override short[] GetValues(object? obj = null) => [(short)ScrollDirection];

    public override BgScrollDirectionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)ScrollDirection);
    }

    public enum BgScrollDirection : short
    {
        DOWN = 1,
        UP = 2,
        RIGHT = 3,
        LEFT = 4,
        DIAGONAL_RIGHT_DOWN = 5,
        DIAGONAL_LEFT_UP = 6,
    }

}
