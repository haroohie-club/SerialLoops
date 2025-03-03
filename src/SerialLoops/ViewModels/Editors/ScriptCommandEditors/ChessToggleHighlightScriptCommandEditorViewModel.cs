using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using HaruhiChokuretsuLib.Util;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChessToggleHighlightScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    [Reactive]
    public SKBitmap Chessboard { get; set; }

    public ObservableCollection<HighlightedChessSpace> HighlightedSpaces { get; } = [];

    public ChessToggleHighlightScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        for (int i = 0; i < Command.Parameters.Count; i++)
        {
            int spaceIndex = ((ChessSpaceScriptParameter)Command.Parameters[i]).SpaceIndex;
            if (spaceIndex == -1)
            {
                break;
            }

            int pieceIndex = ChessPuzzleItem.ConvertSpaceIndexToPieceIndex(spaceIndex);
            if (spaceIndex > 0)
            {
                SKPoint spacePos = ChessPuzzleItem.GetChessPiecePosition(pieceIndex);
                spacePos.Y += 16;
                HighlightedSpaces.Add(new(GetBrush(spaceIndex), spacePos, i, spaceIndex));
            }
        }
    }

    private bool IsRemove(int spaceIndex) => ScriptEditor.CurrentCrossedSpaces.Contains((short)spaceIndex);

    private IImmutableSolidColorBrush GetBrush(int spaceIndex)
    {
        return IsRemove(spaceIndex) ? Brushes.Black : Brushes.Goldenrod;
    }

    public void LoadChessboard()
    {
        Chessboard = ScriptEditor.CurrentChessBoard?.GetChessboard(ScriptEditor.Window.OpenProject);
        foreach (HighlightedChessSpace highlightedSpace in HighlightedSpaces)
        {
            highlightedSpace.Fill = GetBrush(highlightedSpace.SpaceIndex);
        }
    }

    public void BoardClick(Point position)
    {
        int chessPieceIndex = ChessPuzzleItem.GetChessPieceIndexFromPosition(new((float)position.X, (float)position.Y - 16));
        SKPoint spacePos = ChessPuzzleItem.GetChessPiecePosition(chessPieceIndex);
        spacePos.Y += 16;
        HighlightedChessSpace existingSpace = HighlightedSpaces.FirstOrDefault(g => g.Position == spacePos);
        if (!HighlightedSpaces.Remove(existingSpace) && HighlightedSpaces.Count < Command.Parameters.Count)
        {
            int spaceIndex = ChessPuzzleItem.ConvertPieceIndexToSpaceIndex(chessPieceIndex);

            HighlightedSpaces.Add(new(GetBrush(spaceIndex), spacePos, HighlightedSpaces.Count, spaceIndex));
        }

        bool hasValues = true;
        for (int i = 0; i < Command.Parameters.Count; i++)
        {
            if (i < HighlightedSpaces.Count)
            {
                ((ChessSpaceScriptParameter)Command.Parameters[i]).SpaceIndex = (short)HighlightedSpaces[i].SpaceIndex;
                Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[i] = (short)HighlightedSpaces[i].SpaceIndex;
            }
            else if (hasValues)
            {
                hasValues = false;
                ((ChessSpaceScriptParameter)Command.Parameters[i]).SpaceIndex = -1;
                Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[i] = -1;
            }
            else
            {
                ((ChessSpaceScriptParameter)Command.Parameters[i]).SpaceIndex = 0;
                Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[i] = 0;
            }
        }

        ScriptEditor.UpdatePreview();
        Script.UnsavedChanges = true;
    }
}
