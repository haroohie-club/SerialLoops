using System;
using System.Collections.Generic;
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

    public ChessPuzzleItem Clone()
    {
        return new(ChessPuzzle.CastTo<ChessFile>());
    }

    public static SKBitmap GetEmptyChessboard(ArchiveFile<GraphicsFile> grp)
    {
        SKBitmap board = new(256, 192);
        using SKCanvas canvas = new(board);
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
        using SKCanvas canvas = new(board);

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
        using SKCanvas canvas = new(pieceBitmap);
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

    public static int ConvertSpaceIndexToPieceIndex(int spacePos)
    {
        int file = (spacePos >> 4) & 0b111;
        int rank = spacePos & 0b111;
        return rank * 8 + file;
    }

    public static int ConvertPieceIndexToSpaceIndex(int pieceIndex)
    {
        int file = pieceIndex % 8;
        int rank = pieceIndex / 8;
        return file * 16 + rank;
    }

    public static (int file, int rank) PieceIndexToRankAndFile(int pieceIndex) =>
        (pieceIndex % 8, pieceIndex / 8);

    public static int RankAndFileToPieceIndex(int file, int rank) => rank * 8 + file;

    public static SKPoint GetChessPiecePosition(int destPos)
    {
        (int file, int rank) = PieceIndexToRankAndFile(destPos);
        return new(file * 20 + 2, rank * 21 + 1);
    }

    public static SKPoint GetChessSpacePosition(int spacePos)
    {
        return GetChessPiecePosition(ConvertSpaceIndexToPieceIndex(spacePos));
    }

    public static int GetChessPieceIndexFromPosition(SKPoint point)
    {
        int file = (int)(point.X - 4) / 20;
        int rank = (int)(point.Y - 2) / 20;
        return RankAndFileToPieceIndex(file, rank);
    }

    public static int GetChessSpaceIndexFromPosition(SKPoint point)
    {
        return ConvertPieceIndexToSpaceIndex(GetChessPieceIndexFromPosition(new(point.X, point.Y)));
    }

    public List<int> GetGuideSpaces(int spaceIndex)
    {
        List<int> guideSpaces = [];
        int pieceIndex = ConvertSpaceIndexToPieceIndex(spaceIndex);
        (int file, int rank) = PieceIndexToRankAndFile(pieceIndex);
        ChessFile.ChessPiece piece = ChessPuzzle.Chessboard[pieceIndex];

        switch (piece)
        {
            case ChessFile.ChessPiece.WhiteKing:
            case ChessFile.ChessPiece.BlackKing:
                for (int x = file - 1; x <= file + 1; x++)
                {
                    for (int y = rank - 1; y <= rank + 1; y++)
                    {
                        if (y >= 0 && x >= 0 && y < 8 && x < 8 && !(y == rank && x == file))
                        {
                            guideSpaces.Add(RankAndFileToPieceIndex(x, y));
                        }
                    }
                }
                break;

            case ChessFile.ChessPiece.WhiteQueen:
            case ChessFile.ChessPiece.BlackQueen:
                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        if (RookCheck(x, y, file, rank) || BishopCheck(x, y, file, rank))
                        {
                            guideSpaces.Add(RankAndFileToPieceIndex(x, y));
                        }
                    }
                }
                break;

            case ChessFile.ChessPiece.WhiteRookLeft:
            case ChessFile.ChessPiece.WhiteRookRight:
            case ChessFile.ChessPiece.BlackRookLeft:
            case ChessFile.ChessPiece.BlackRookRight:
                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        if (RookCheck(x, y, file, rank))
                        {
                            guideSpaces.Add(RankAndFileToPieceIndex(x, y));
                        }
                    }
                }
                break;

            case ChessFile.ChessPiece.WhiteBishopLeft:
            case ChessFile.ChessPiece.WhiteBishopRight:
            case ChessFile.ChessPiece.BlackBishopLeft:
            case ChessFile.ChessPiece.BlackBishopRight:
                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        if (BishopCheck(x, y, file, rank))
                        {
                            guideSpaces.Add(RankAndFileToPieceIndex(x, y));
                        }
                    }
                }
                break;

            case ChessFile.ChessPiece.WhiteKnightLeft:
            case ChessFile.ChessPiece.WhiteKnightRight:
            case ChessFile.ChessPiece.BlackKnightLeft:
            case ChessFile.ChessPiece.BlackKnightRight:
                for (int x = file - 2; x <= file + 2; x++)
                {
                    for (int y = rank - 2; y <= rank + 2; y++)
                    {
                        if (y >= 0 && x >= 0 && y < 8 && x < 8 &&
                            ((Math.Abs(y - rank) == 1 && Math.Abs(x - file) == 2) ||
                             (Math.Abs(y - rank) == 2 && Math.Abs(x - file) == 1)) &&
                             !(x == file && y == rank))
                        {
                            guideSpaces.Add(RankAndFileToPieceIndex(x, y));
                        }
                    }
                }
                break;

            case ChessFile.ChessPiece.WhitePawnA:
            case ChessFile.ChessPiece.WhitePawnB:
            case ChessFile.ChessPiece.WhitePawnC:
            case ChessFile.ChessPiece.WhitePawnD:
            case ChessFile.ChessPiece.WhitePawnE:
            case ChessFile.ChessPiece.WhitePawnF:
            case ChessFile.ChessPiece.WhitePawnG:
            case ChessFile.ChessPiece.WhitePawnH:
                if (rank == 6)
                {
                    guideSpaces.Add(RankAndFileToPieceIndex(file, rank - 2));
                }
                guideSpaces.Add(RankAndFileToPieceIndex(file, rank - 1));
                break;
            case ChessFile.ChessPiece.BlackPawnA:
            case ChessFile.ChessPiece.BlackPawnB:
            case ChessFile.ChessPiece.BlackPawnC:
            case ChessFile.ChessPiece.BlackPawnD:
            case ChessFile.ChessPiece.BlackPawnE:
            case ChessFile.ChessPiece.BlackPawnF:
            case ChessFile.ChessPiece.BlackPawnH:
                if (rank == 1)
                {
                    guideSpaces.Add(RankAndFileToPieceIndex(file, rank + 2));
                }
                guideSpaces.Add(RankAndFileToPieceIndex(file, rank + 1));
                break;

            default:
            case ChessFile.ChessPiece.Empty:
                break;
        }

        return guideSpaces;

        static bool BishopCheck(int x, int y, int file, int rank)
        {
            return Math.Abs(file - x) == Math.Abs(rank - y) && !(x == file && y == rank);
        }

        static bool RookCheck(int x, int y, int file, int rank)
        {
            return (x == file || y == rank) && !(x == file && y == rank);
        }
    }

    public override string ToString() => DisplayName;
}
