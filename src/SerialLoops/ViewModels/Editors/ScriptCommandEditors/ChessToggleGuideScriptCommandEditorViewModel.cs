using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChessToggleGuideScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    [Reactive]
    public SKBitmap Chessboard { get; set; }

    public ObservableCollection<HighlightedChessSpace> GuidePieces { get; } = [];

    private bool _clearAll;

    public bool ClearAll
    {
        get => _clearAll;
        set
        {
            this.RaiseAndSetIfChanged(ref _clearAll, value);
            GuidePieces.Clear();
            if (_clearAll)
            {
                ((ChessSpaceScriptParameter)Command.Parameters[0]).SpaceIndex = 131;
                Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[0] = 131;
                for (int i = 1; i < 4; i++)
                {
                    ((ChessSpaceScriptParameter)Command.Parameters[i]).SpaceIndex = 0;
                    Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[i] = 0;
                }
            }
            else
            {
                ((ChessSpaceScriptParameter)Command.Parameters[0]).SpaceIndex = 0;
                Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[0] = 0;
            }
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ChessToggleGuideScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        for (int i = 0; i < 4; i++)
        {
            short spaceIndex = ((ChessSpaceScriptParameter)Command.Parameters[i]).SpaceIndex;
            if (spaceIndex == 131)
            {
                _clearAll = true;
                continue;
            }
            int pieceIndex = ChessPuzzleItem.ConvertSpaceIndexToPieceIndex(spaceIndex);
            if (pieceIndex > 0)
            {
                SKPoint spacePos = ChessPuzzleItem.GetChessPiecePosition(pieceIndex);
                spacePos.Y += 16;
                GuidePieces.Add(new(GetBrush(spaceIndex), spacePos, i, spaceIndex));
            }
        }
    }

    public void LoadChessboard()
    {
        Chessboard = ScriptEditor.CurrentChessBoard?.GetChessboard(ScriptEditor.Window.OpenProject);
        foreach (HighlightedChessSpace guideSpace in GuidePieces)
        {
            guideSpace.Fill = GetBrush(guideSpace.SpaceIndex);
        }
    }

    public void BoardClick(Point position)
    {
        if (ClearAll)
        {
            return;
        }

        int chessPieceIndex = ChessPuzzleItem.GetChessPieceIndexFromPosition(new((float)position.X, (float)position.Y - 16));
        if (ScriptEditor.CurrentChessBoard.ChessPuzzle.Chessboard[chessPieceIndex] == ChessFile.ChessPiece.Empty)
        {
            return;
        }

        SKPoint spacePos = ChessPuzzleItem.GetChessPiecePosition(chessPieceIndex);
        spacePos.Y += 16;
        HighlightedChessSpace existingSpace = GuidePieces.FirstOrDefault(g => g.Position == spacePos);
        if (!GuidePieces.Remove(existingSpace) && GuidePieces.Count <= 1)
        {
            int spaceIndex = ChessPuzzleItem.ConvertPieceIndexToSpaceIndex(chessPieceIndex);

            // TODO: update documentation to reflect that param 1 is for toggling on and param 2 is for toggling off, lol
            int newParamIndex = IsRemove(spaceIndex) ? 0 : 1;
            GuidePieces.Add(new(GetBrush(spaceIndex), spacePos, newParamIndex, spaceIndex));
            ((ChessSpaceScriptParameter)Command.Parameters[newParamIndex]).SpaceIndex = (short)spaceIndex;
            Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[newParamIndex] = (short)spaceIndex;
        }
        else
        {
            ((ChessSpaceScriptParameter)Command.Parameters[existingSpace!.ParamIndex]).SpaceIndex = 0;
            Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[existingSpace!.ParamIndex] = 0;
        }

        ScriptEditor.UpdatePreview();
        Script.UnsavedChanges = true;
    }

    private bool IsRemove(int spaceIndex) => ScriptEditor.CurrentGuidePieces.Contains((short)spaceIndex);

    private IImmutableSolidColorBrush GetBrush(int spaceIndex)
    {
        return IsRemove(spaceIndex) ? Brushes.Black : Brushes.Red;
    }
}

public class HighlightedChessSpace(IImmutableSolidColorBrush fill, SKPoint position, int paramIndex, int spaceIndex) : ReactiveObject
{
    [Reactive]
    public IImmutableSolidColorBrush Fill { get; set; } = fill;

    public SKPoint Position { get; } = position;

    public int ParamIndex { get; } = paramIndex;

    public int SpaceIndex { get; } = spaceIndex;
}
