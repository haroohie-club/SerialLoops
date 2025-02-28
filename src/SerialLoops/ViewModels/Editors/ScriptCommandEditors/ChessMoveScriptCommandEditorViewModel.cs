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
    public SKBitmap Chessboard => ScriptEditor.CurrentChessBoard?.GetChessboard(ScriptEditor.Window.OpenProject);

    private SKPoint? _whiteSpaceBeginPoint;
    public SKPoint? WhiteSpaceBeginPoint
    {
        get => _whiteSpaceBeginPoint;
        set
        {
            _whiteSpaceBeginPoint = value;
            WhiteSpaceBeginX = (_whiteSpaceBeginPoint?.X ?? 0) - 2;
            WhiteSpaceBeginY = (_whiteSpaceBeginPoint?.Y ?? 0) + 8;
        }
    }
    private SKPoint? _whiteSpaceEndPoint;
    public SKPoint? WhiteSpaceEndPoint
    {
        get => _whiteSpaceEndPoint;
        set
        {
            _whiteSpaceEndPoint = value;
            WhiteSpaceEndX = (_whiteSpaceEndPoint?.X ?? 0) - 2;
            WhiteSpaceEndY = (_whiteSpaceEndPoint?.Y ?? 0) + 8;
        }
    }

    private SKPoint? _blackSpaceBeginPoint;
    public SKPoint? BlackSpaceBeginPoint
    {
        get => _blackSpaceBeginPoint;
        set
        {
            _blackSpaceBeginPoint = value;
            BlackSpaceBeginX = (_blackSpaceBeginPoint?.X ?? 0) - 2;
            BlackSpaceBeginY = (_blackSpaceBeginPoint?.Y ?? 0) + 8;
        }
    }
    private SKPoint? _blackSpaceEndPoint;
    public SKPoint? BlackSpaceEndPoint
    {
        get => _blackSpaceEndPoint;
        set
        {
            _blackSpaceEndPoint = value;
            BlackSpaceEndX = (_blackSpaceEndPoint?.X ?? 0) - 2;
            BlackSpaceEndY = (_blackSpaceEndPoint?.Y ?? 0) + 8;
        }
    }

    [Reactive]
    public float WhiteSpaceBeginX { get; set; }
    [Reactive]
    public float WhiteSpaceBeginY { get; set; }
    [Reactive]
    public float WhiteSpaceEndX { get; set; }
    [Reactive]
    public float WhiteSpaceEndY { get; set; }

    [Reactive]
    public float BlackSpaceBeginX { get; set; }
    [Reactive]
    public float BlackSpaceBeginY { get; set; }
    [Reactive]
    public float BlackSpaceEndX { get; set; }
    [Reactive]
    public float BlackSpaceEndY { get; set; }

    public ChessMoveScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        short whiteSpaceBeginIndex = ((ChessSpaceScriptParameter)command.Parameters[0]).SpaceIndex;
        short whiteSpaceEndIndex = ((ChessSpaceScriptParameter)command.Parameters[1]).SpaceIndex;
        short blackSpaceBeginIndex = ((ChessSpaceScriptParameter)command.Parameters[2]).SpaceIndex;
        short blackSpaceEndIndex =  ((ChessSpaceScriptParameter)command.Parameters[3]).SpaceIndex;

        if (whiteSpaceBeginIndex != 0 || whiteSpaceEndIndex != 0)
        {
            WhiteSpaceBeginPoint = ChessPuzzleItem.GetChessSpacePosition(whiteSpaceBeginIndex);
            WhiteSpaceEndPoint = ChessPuzzleItem.GetChessSpacePosition(whiteSpaceEndIndex);
        }

        if (blackSpaceBeginIndex != 0 || blackSpaceEndIndex != 0)
        {
            BlackSpaceBeginPoint = ChessPuzzleItem.GetChessSpacePosition(blackSpaceBeginIndex);
            BlackSpaceEndPoint = ChessPuzzleItem.GetChessSpacePosition(blackSpaceEndIndex);
        }
    }

    public void WhiteBoardClick(Point position)
    {
        if (WhiteSpaceEndPoint is not null)
        {
            WhiteSpaceEndPoint = null;
            short newIndex = (short)ChessPuzzleItem.GetChessSpaceIndexFromPosition(new((float)position.X, (float)position.Y));
            if (newIndex == ((ChessSpaceScriptParameter)Command.Parameters[0]).SpaceIndex)
            {
                return;
            }

            WhiteSpaceBeginPoint = ChessPuzzleItem.GetChessSpacePosition(newIndex);

            ((ChessSpaceScriptParameter)Command.Parameters[0]).SpaceIndex = newIndex;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = newIndex;
        }
        else
        {
            short newIndex = (short)ChessPuzzleItem.GetChessSpaceIndexFromPosition(new((float)position.X, (float)position.Y));
            if (newIndex == ((ChessSpaceScriptParameter)Command.Parameters[0]).SpaceIndex || newIndex == ((ChessSpaceScriptParameter)Command.Parameters[1]).SpaceIndex)
            {
                return;
            }

            WhiteSpaceEndPoint = ChessPuzzleItem.GetChessSpacePosition(newIndex);

            ((ChessSpaceScriptParameter)Command.Parameters[1]).SpaceIndex = newIndex;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = newIndex;
        }

        Script.UnsavedChanges = true;
        ScriptEditor.UpdatePreview();
    }

    public void BlackBoardClick(Point position)
    {
        if (BlackSpaceEndPoint is not null)
        {
            BlackSpaceEndPoint = null;
            short newIndex = (short)ChessPuzzleItem.GetChessSpaceIndexFromPosition(new((float)position.X, (float)position.Y));
            if (newIndex == ((ChessSpaceScriptParameter)Command.Parameters[2]).SpaceIndex)
            {
                return;
            }

            BlackSpaceBeginPoint = ChessPuzzleItem.GetChessSpacePosition(newIndex);

            ((ChessSpaceScriptParameter)Command.Parameters[2]).SpaceIndex = newIndex;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = newIndex;
        }
        else
        {
            short newIndex = (short)ChessPuzzleItem.GetChessSpaceIndexFromPosition(new((float)position.X, (float)position.Y));
            if (newIndex == ((ChessSpaceScriptParameter)Command.Parameters[2]).SpaceIndex || newIndex == ((ChessSpaceScriptParameter)Command.Parameters[3]).SpaceIndex)
            {
                return;
            }

            BlackSpaceEndPoint = ChessPuzzleItem.GetChessSpacePosition(newIndex);

            ((ChessSpaceScriptParameter)Command.Parameters[3]).SpaceIndex = newIndex;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[3] = newIndex;
        }

        Script.UnsavedChanges = true;
        ScriptEditor.UpdatePreview();
    }
}
