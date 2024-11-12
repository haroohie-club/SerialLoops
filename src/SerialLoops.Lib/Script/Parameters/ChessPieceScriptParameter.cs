using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ChessPieceScriptParameter(string name, short chessPiece) : ScriptParameter(name, ParameterType.CHESS_PIECE)
{
    public short ChessPiece { get; set; } = chessPiece;
    public override short[] GetValues(object? obj = null) => [ChessPiece];

    public override ChessPieceScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, ChessPiece);
    }
}
