namespace SerialLoops.Lib.Script
{
    public class ChessFileScriptParameter : ScriptParameter
    {
        public short ChessFileIndex { get; set; }

        public ChessFileScriptParameter(string name, short chessFileIndex) : base(name, ParameterType.CHESS_FILE)
        {
            ChessFileIndex = chessFileIndex;
        }
    }
}
