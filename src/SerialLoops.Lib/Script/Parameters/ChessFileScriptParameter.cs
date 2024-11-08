using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ChessFileScriptParameter : ScriptParameter
{
    public short ChessFileIndex { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { ChessFileIndex };

    public ChessFileScriptParameter(string name, short chessFileIndex) : base(name, ParameterType.CHESS_FILE)
    {
        ChessFileIndex = chessFileIndex;
    }

    public override ChessFileScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, ChessFileIndex);
    }
}