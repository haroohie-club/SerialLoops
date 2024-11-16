using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items;

public class ChessPuzzleItem : Item
{
    public ChessFile ChessPuzzle { get; set; }

    public ChessPuzzleItem(ChessFile chessFile) : base(chessFile.Name[..^1], ItemType.Chess_Puzzle)
    {
        ChessPuzzle = chessFile;
    }

    public override void Refresh(Project project, ILogger log)
    {
    }

    public SKBitmap GetChessboard(ArchiveFile<GraphicsFile> grp)
    {
        SKBitmap board = new(256, 192);
        SKCanvas canvas = new(board);

        SKBitmap chs0White = grp.GetFileByName("CHS_SYS_00DNX").GetImage(transparentIndex: 0);
        SKBitmap chs0Black = grp.GetFileByName("CHS_SYS_00DNX").GetImage(transparentIndex: 16, paletteOffset: 16);
        SKBitmap chs1 = grp.GetFileByName("CHS_SYS_01DNX").GetImage(transparentIndex: 0);

        canvas.DrawBitmap(chs1, new(0,0,80,80), new SKRect(0,16,80,96));
        canvas.DrawBitmap(chs1, new(1,0,81,80), new SKRect(80,16,160,96));
        canvas.DrawBitmap(chs1, new(0,1,80,81), new SKRect(0,96,80,186));
        canvas.DrawBitmap(chs1, new(1,1,81,81), new SKRect(80,96,160,186));

        for (int i = 0; i < ChessPuzzle.Chessboard.Length; i++)
        {
            switch (ChessPuzzle.Chessboard[i])
            {
                case ChessFile.ChessPiece.WhiteKing:
                    DrawChessPiece(canvas, chs0White, 0, i);
                    break;

                case ChessFile.ChessPiece.WhiteQueen:
                    DrawChessPiece(canvas, chs0White, 16, i);
                    break;

                case ChessFile.ChessPiece.WhiteBishopLeft:
                case ChessFile.ChessPiece.WhiteBishopRight:
                    DrawChessPiece(canvas, chs0White, 32, i);
                    break;

                case ChessFile.ChessPiece.WhiteRookLeft:
                case ChessFile.ChessPiece.WhiteRookRight:
                    DrawChessPiece(canvas, chs0White, 48, i);
                    break;

                case ChessFile.ChessPiece.WhiteKnightLeft:
                case ChessFile.ChessPiece.WhiteKnightRight:
                    DrawChessPiece(canvas, chs0White, 64, i);
                    break;

                case ChessFile.ChessPiece.WhitePawnA:
                case ChessFile.ChessPiece.WhitePawnB:
                case ChessFile.ChessPiece.WhitePawnC:
                case ChessFile.ChessPiece.WhitePawnD:
                case ChessFile.ChessPiece.WhitePawnE:
                case ChessFile.ChessPiece.WhitePawnF:
                case ChessFile.ChessPiece.WhitePawnG:
                case ChessFile.ChessPiece.WhitePawnH:
                    DrawChessPiece(canvas, chs0White, 80, i);
                    break;

                case ChessFile.ChessPiece.BlackKing:
                    DrawChessPiece(canvas, chs0Black, 0, i);
                    break;

                case ChessFile.ChessPiece.BlackQueen:
                    DrawChessPiece(canvas, chs0Black, 16, i);
                    break;

                case ChessFile.ChessPiece.BlackBishopLeft:
                case ChessFile.ChessPiece.BlackBishopRight:
                    DrawChessPiece(canvas, chs0Black, 32, i);
                    break;

                case ChessFile.ChessPiece.BlackRookLeft:
                case ChessFile.ChessPiece.BlackRookRight:
                    DrawChessPiece(canvas, chs0Black, 48, i);
                    break;

                case ChessFile.ChessPiece.BlackKnightLeft:
                case ChessFile.ChessPiece.BlackKnightRight:
                    DrawChessPiece(canvas, chs0Black, 64, i);
                    break;

                case ChessFile.ChessPiece.BlackPawnA:
                case ChessFile.ChessPiece.BlackPawnB:
                case ChessFile.ChessPiece.BlackPawnC:
                case ChessFile.ChessPiece.BlackPawnD:
                case ChessFile.ChessPiece.BlackPawnE:
                case ChessFile.ChessPiece.BlackPawnF:
                case ChessFile.ChessPiece.BlackPawnG:
                case ChessFile.ChessPiece.BlackPawnH:
                    DrawChessPiece(canvas, chs0Black, 80, i);
                    break;

                default:
                    continue;
            }
        }

        canvas.Flush();
        return board;
    }

    private static void DrawChessPiece(SKCanvas canvas, SKBitmap source, int sourceOffset, int destPos)
    {
        canvas.DrawBitmap(source, new SKRect(sourceOffset, 0, sourceOffset + 16, 32),
            new SKRect(destPos * 20 % 160 + 2, destPos / 8 * 21 + 4,destPos * 20 % 160 + 18, destPos / 8 * 21 + 36));
    }
}
