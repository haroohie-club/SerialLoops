using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items;

public class ChessPuzzleItem : Item
{
    public ChessFile ChessPuzzle { get; set; }

    public ChessPuzzleItem(ChessFile chessFile, ArchiveFile<GraphicsFile> grp) : base(chessFile.Name[..^1], ItemType.Chess_Puzzle)
    {
        ChessPuzzle = chessFile;
    }

    public override void Refresh(Project project, ILogger log)
    {
    }

    public static SKBitmap GetEmptyChessboard(ArchiveFile<GraphicsFile> grp)
    {
        SKBitmap board = new(256, 192);
        SKCanvas canvas = new(board);
        SKBitmap chs1 = grp.GetFileByName("CHS_SYS_01DNX").GetImage(transparentIndex: 0);

        canvas.DrawBitmap(chs1, new(0,0,80,80), new SKRect(0,16,80,96));
        canvas.DrawBitmap(chs1, new(1,0,81,80), new SKRect(80,16,160,96));
        canvas.DrawBitmap(chs1, new(0,1,80,81), new SKRect(0,96,80,186));
        canvas.DrawBitmap(chs1, new(1,1,81,81), new SKRect(80,96,160,186));
        canvas.Flush();

        return board;
    }

    public SKBitmap GetChessboard(Project project)
    {
        SKBitmap board = new(256, 192);
        SKCanvas canvas = new(board);

        SKBitmap emptyBoard = GetEmptyChessboard(project.Grp);
        canvas.DrawBitmap(emptyBoard, 0, 0);

        for (int i = 0; i < ChessPuzzle.Chessboard.Length; i++)
        {
            DrawChessPiece(canvas, project.ChessPieceImages[ChessPuzzle.Chessboard[i]], i);
        }

        canvas.Flush();
        return board;
    }

    public static SKBitmap GetChessPiece(ChessFile.ChessPiece piece, ArchiveFile<GraphicsFile> grp)
    {
        SKBitmap pieceBitmap = new(16, 32);
        SKCanvas canvas = new(pieceBitmap);
        SKBitmap chs0 = (byte)piece < 0x80
            ? grp.GetFileByName("CHS_SYS_00DNX").GetImage(transparentIndex: 0)
            : grp.GetFileByName("CHS_SYS_00DNX").GetImage(transparentIndex: 16, paletteOffset: 16);

        int pieceOffset = GetChessPieceOffset(piece);
        canvas.DrawBitmap(chs0, new(pieceOffset, 0, pieceOffset + 16, 32), new SKRect(0, 0, 16, 32));
        canvas.Flush();

        return pieceBitmap;
    }

    private static int GetChessPieceOffset(ChessFile.ChessPiece piece)
    {
        switch (piece)
        {
            case ChessFile.ChessPiece.WhiteKing:
            case ChessFile.ChessPiece.BlackKing:
                return 0;

            case ChessFile.ChessPiece.WhiteQueen:
            case ChessFile.ChessPiece.BlackQueen:
                return 16;

            case ChessFile.ChessPiece.WhiteBishopLeft:
            case ChessFile.ChessPiece.WhiteBishopRight:
            case ChessFile.ChessPiece.BlackBishopLeft:
            case ChessFile.ChessPiece.BlackBishopRight:
                return 32;

            case ChessFile.ChessPiece.WhiteRookLeft:
            case ChessFile.ChessPiece.WhiteRookRight:
            case ChessFile.ChessPiece.BlackRookLeft:
            case ChessFile.ChessPiece.BlackRookRight:
                return 48;


            case ChessFile.ChessPiece.WhiteKnightLeft:
            case ChessFile.ChessPiece.WhiteKnightRight:
            case ChessFile.ChessPiece.BlackKnightLeft:
            case ChessFile.ChessPiece.BlackKnightRight:
                return 64;

            case ChessFile.ChessPiece.WhitePawnA:
            case ChessFile.ChessPiece.WhitePawnB:
            case ChessFile.ChessPiece.WhitePawnC:
            case ChessFile.ChessPiece.WhitePawnD:
            case ChessFile.ChessPiece.WhitePawnE:
            case ChessFile.ChessPiece.WhitePawnF:
            case ChessFile.ChessPiece.WhitePawnG:
            case ChessFile.ChessPiece.WhitePawnH:
            case ChessFile.ChessPiece.BlackPawnA:
            case ChessFile.ChessPiece.BlackPawnB:
            case ChessFile.ChessPiece.BlackPawnC:
            case ChessFile.ChessPiece.BlackPawnD:
            case ChessFile.ChessPiece.BlackPawnE:
            case ChessFile.ChessPiece.BlackPawnF:
            case ChessFile.ChessPiece.BlackPawnG:
            case ChessFile.ChessPiece.BlackPawnH:
                return 80;

            case ChessFile.ChessPiece.Empty:
            default:
                return -16;
        }
    }

    private static void DrawChessPiece(SKCanvas canvas, SKBitmap source, int destPos)
    {
        SKPoint destPoint = GetChessPiecePosition(destPos);
        canvas.DrawBitmap(source, new(0, 0, 16, 32),
            new SKRect(destPoint.X, destPoint.Y,destPoint.X + 16, destPoint.Y + 32));
    }

    public static SKPoint GetChessPiecePosition(int destPos)
    {
        return new(destPos * 20 % 160 + 2, destPos / 8 * 21 + 4);
    }

    public static int GetChessPieceIndexFromPosition(SKPoint point)
    {
        return ((int)point.Y - 4) * 21 / 8 + ((int)point.X - 2) * 20;
    }
}
