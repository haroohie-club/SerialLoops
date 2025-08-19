using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class BgScrollDirectionScriptParameter : ScriptParameter
{
    public BgScrollDirection ScrollDirection { get; set; }
    public override short[] GetValues(object obj = null) => [(short)ScrollDirection];

    public override string GetValueString(Project project)
    {
        return project.Localize(ScrollDirection.ToString());
    }

    public BgScrollDirectionScriptParameter(string name, short scrollDirection) : base(name, ParameterType.BG_SCROLL_DIRECTION)
    {
        ScrollDirection = (BgScrollDirection)scrollDirection;
    }

    public override BgScrollDirectionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)ScrollDirection);
    }

    public enum BgScrollDirection : short
    {
        BgScrollDown = 1,
        BgScrollUp = 2,
        BgScrollRight = 3,
        BgScrollLeft = 4,
        BgScrollDiagonalRightUp = 5,
        BgScrollDiagonalLeftUp = 6,
    }

}
