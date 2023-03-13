namespace SerialLoops.Lib.Script.Parameters
{
    public class ChessPieceScriptParameter : ScriptParameter
    {
        public short ChessPiece { get; set; }

        public ChessPieceScriptParameter(string name, short chessPiece) : base(name, ParameterType.CHESS_PIECE)
        {
            ChessPiece = chessPiece;
        }

        public override ChessPieceScriptParameter Clone()
        {
            return new(Name, ChessPiece);
        }
    }
}
