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

public class ChessToggleCrossScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    [Reactive]
    public SKBitmap Chessboard { get; set; }

    public ObservableCollection<HighlightedChessSpace> CrossedSpaces { get; } = [];

    public ChessToggleCrossScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
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
                CrossedSpaces.Add(new(Brushes.Transparent, spacePos, i, spaceIndex) { SvgFill = GetSvgFill(spaceIndex)});
            }
        }
    }

    private bool IsRemove(int spaceIndex) => ScriptEditor.CurrentHighlightedSpaces.Contains((short)spaceIndex);

    private string GetSvgFill(int spaceIndex)
    {
        return IsRemove(spaceIndex) ? "path { fill: #000000 !important }" : "path { fill: #FF0000 !important }";
    }

    public void LoadChessboard()
    {
        Chessboard = ScriptEditor.CurrentChessBoard?.GetChessboard(ScriptEditor.Window.OpenProject);
        foreach (HighlightedChessSpace highlightedSpace in CrossedSpaces)
        {
            highlightedSpace.Fill = Brushes.Transparent;
            highlightedSpace.SvgFill = GetSvgFill(highlightedSpace.SpaceIndex);
        }
    }

    public void BoardClick(Point position)
    {
        int chessPieceIndex = ChessPuzzleItem.GetChessPieceIndexFromPosition(new((float)position.X, (float)position.Y - 16));
        SKPoint spacePos = ChessPuzzleItem.GetChessPiecePosition(chessPieceIndex);
        spacePos.Y += 16;
        HighlightedChessSpace existingSpace = CrossedSpaces.FirstOrDefault(g => g.Position == spacePos);
        if (!CrossedSpaces.Remove(existingSpace) && CrossedSpaces.Count < Command.Parameters.Count)
        {
            int spaceIndex = ChessPuzzleItem.ConvertPieceIndexToSpaceIndex(chessPieceIndex);

            CrossedSpaces.Add(new(Brushes.Transparent, spacePos, CrossedSpaces.Count, spaceIndex) { SvgFill = GetSvgFill(spaceIndex)});
        }

        bool hasValues = true;
        for (int i = 0; i < Command.Parameters.Count; i++)
        {
            if (i < CrossedSpaces.Count)
            {
                ((ChessSpaceScriptParameter)Command.Parameters[i]).SpaceIndex = (short)CrossedSpaces[i].SpaceIndex;
                Command.Script.ScriptSections[Command.Script.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[i] = (short)CrossedSpaces[i].SpaceIndex;
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
