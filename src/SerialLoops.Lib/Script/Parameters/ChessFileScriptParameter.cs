using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ChessFileScriptParameter(string name, short chessFileIndex)
    : ScriptParameter(name, ParameterType.CHESS_FILE)
{
    public short ChessFileIndex { get; set; } = chessFileIndex;
    public override short[] GetValues(object? obj = null) => [ChessFileIndex];

    public override ChessFileScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, ChessFileIndex);
    }
}
