using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ChessSpaceScriptParameter(string name, short spaceIndex) : ScriptParameter(name, ParameterType.CHESS_SPACE)
{
    public short SpaceIndex { get; set; } = spaceIndex;
    public override short[] GetValues(object? obj = null) => [SpaceIndex];

    public override ChessSpaceScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, SpaceIndex);
    }

}
