using Avalonia;
using HaruhiChokuretsuLib.Util;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChessMoveScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    [Reactive]
    public SKBitmap Chessboard { get; set; }

    private SKPoint? _move1BeginPoint;
    public SKPoint? Move1BeginPoint
    {
        get => _move1BeginPoint;
        set
        {
            _move1BeginPoint = value;
            Move1BeginX = (_move1BeginPoint?.X ?? 0) - 2;
            Move1BeginY = (_move1BeginPoint?.Y ?? 0) + 8;
        }
    }
    private SKPoint? _move1EndPoint;
    public SKPoint? Move1EndPoint
    {
        get => _move1EndPoint;
        set
        {
            _move1EndPoint = value;
            Move1EndX = (_move1EndPoint?.X ?? 0) - 2;
            Move1EndY = (_move1EndPoint?.Y ?? 0) + 8;
        }
    }

    private SKPoint? _move2BeginPoint;
    public SKPoint? Move2BeginPoint
    {
        get => _move2BeginPoint;
        set
        {
            _move2BeginPoint = value;
            Move2BeginX = (_move2BeginPoint?.X ?? 0) - 2;
            Move2BeginY = (_move2BeginPoint?.Y ?? 0) + 8;
        }
    }
    private SKPoint? _move2EndPoint;
    public SKPoint? Move2EndPoint
    {
        get => _move2EndPoint;
        set
        {
            _move2EndPoint = value;
            Move2EndX = (_move2EndPoint?.X ?? 0) - 2;
            Move2EndY = (_move2EndPoint?.Y ?? 0) + 8;
        }
    }

    [Reactive]
    public float Move1BeginX { get; set; }
    [Reactive]
    public float Move1BeginY { get; set; }
    [Reactive]
    public float Move1EndX { get; set; }
    [Reactive]
    public float Move1EndY { get; set; }

    [Reactive]
    public float Move2BeginX { get; set; }
    [Reactive]
    public float Move2BeginY { get; set; }
    [Reactive]
    public float Move2EndX { get; set; }
    [Reactive]
    public float Move2EndY { get; set; }

    public ChessMoveScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        short move1SpaceBeginIndex = ((ChessSpaceScriptParameter)command.Parameters[0]).SpaceIndex;
        short move1SpaceEndIndex = ((ChessSpaceScriptParameter)command.Parameters[1]).SpaceIndex;
        short move2SpaceBeginIndex = ((ChessSpaceScriptParameter)command.Parameters[2]).SpaceIndex;
        short move2SpaceEndIndex =  ((ChessSpaceScriptParameter)command.Parameters[3]).SpaceIndex;

        if (move1SpaceBeginIndex != move1SpaceEndIndex)
        {
            Move1BeginPoint = ChessPuzzleItem.GetChessSpacePosition(move1SpaceBeginIndex);
            Move1EndPoint = ChessPuzzleItem.GetChessSpacePosition(move1SpaceEndIndex);
        }

        if (move2SpaceBeginIndex != move2SpaceEndIndex)
        {
            Move2BeginPoint = ChessPuzzleItem.GetChessSpacePosition(move2SpaceBeginIndex);
            Move2EndPoint = ChessPuzzleItem.GetChessSpacePosition(move2SpaceEndIndex);
        }
    }

    public void LoadChessboard()
    {
        Chessboard = ScriptEditor.CurrentChessBoard?.GetChessboard(ScriptEditor.Window.OpenProject);
    }

    public void Board1Click(Point position)
    {
        BoardClick(position, 0);
    }

    public void Board2Click(Point position)
    {
        BoardClick(position, 2);
    }

    private void BoardClick(Point position, int paramStartIndex)
    {
        if ((paramStartIndex == 0 ? Move1EndPoint : Move2EndPoint) is not null)
        {
            int chessPieceIndex = ChessPuzzleItem.GetChessPieceIndexFromPosition(new((float)position.Y, (float)position.X));
            if (chessPieceIndex > ScriptEditor.CurrentChessBoard.ChessPuzzle.Chessboard.Length)
            {
                return;
            }
            if (paramStartIndex == 0)
            {
                Move1EndPoint = null;
            }
            else
            {
                Move2EndPoint = null;
            }
            short newIndex = (short)ChessPuzzleItem.ConvertPieceIndexToSpaceIndex(chessPieceIndex);

            if (paramStartIndex == 0)
            {
                Move1BeginPoint = ChessPuzzleItem.GetChessSpacePosition(newIndex);
            }
            else
            {
                Move2BeginPoint = ChessPuzzleItem.GetChessSpacePosition(newIndex);
            }

            ((ChessSpaceScriptParameter)Command.Parameters[paramStartIndex]).SpaceIndex = newIndex;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[paramStartIndex] = newIndex;
        }
        else
        {
            int chessPieceIndex = ChessPuzzleItem.GetChessPieceIndexFromPosition(new((float)position.Y, (float)position.X));
            if (chessPieceIndex > ScriptEditor.CurrentChessBoard.ChessPuzzle.Chessboard.Length)
            {
                return;
            }
            short newIndex = (short)ChessPuzzleItem.ConvertPieceIndexToSpaceIndex(chessPieceIndex);
            if (newIndex == ((ChessSpaceScriptParameter)Command.Parameters[paramStartIndex]).SpaceIndex)
            {
                return;
            }

            if (paramStartIndex == 0)
            {
                Move1EndPoint = ChessPuzzleItem.GetChessSpacePosition(newIndex);
            }
            else
            {
                Move2EndPoint = ChessPuzzleItem.GetChessSpacePosition(newIndex);
            }

            ((ChessSpaceScriptParameter)Command.Parameters[paramStartIndex + 1]).SpaceIndex = newIndex;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[paramStartIndex + 1] = newIndex;
        }

        Script.UnsavedChanges = true;
        ScriptEditor.UpdatePreview();
    }
}
