namespace SerialLoops.Lib.Script.Parameters
{
    public class ChessFileScriptParameter : ScriptParameter
    {
        public short ChessFileIndex { get; set; }

        public ChessFileScriptParameter(string name, short chessFileIndex) : base(name, ParameterType.CHESS_FILE)
        {
            ChessFileIndex = chessFileIndex;
        }

        public override ChessFileScriptParameter Clone()
        {
            return new(Name, ChessFileIndex);
        }
    }
}
