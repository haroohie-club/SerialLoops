using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using HaruhiChokuretsuLib.Archive.Data;
using SerialLoops.ViewModels.Editors;

namespace SerialLoops.Behaviors;

public class ItemsControlDropHandler : DropHandlerBase
{
    private bool ValidateChess(ItemsControl itemsControl, DragEventArgs e, ChessPieceOnBoard sourceContext, ChessPuzzleEditorViewModel targetContext, bool bExecute)
    {
        if (itemsControl.GetVisualAt(e.GetPosition(itemsControl)) is not Control { DataContext: ChessPieceOnBoard targetItem })
        {
            return false;
        }

        if (sourceContext.Piece == ChessFile.ChessPiece.Empty)
        {
            return false;
        }

        ObservableCollection<ChessPieceOnBoard> pieces = targetContext.Pieces;
        int sourceIndex = pieces.IndexOf(sourceContext);
        int targetIndex = pieces.IndexOf(targetItem);

        if (sourceIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        switch (e.DragEffects)
        {
            case DragDropEffects.Move:
                if (bExecute)
                {
                    targetContext.MovePiece(sourceIndex, targetIndex);
                }
                return true;

            default:
                return false;
        }
    }

    public override bool Validate(object sender, DragEventArgs e, object sourceContext, object targetContext,
        object state)
    {
        if (e.Source is Control && sender is ItemsControl itemsControl)
        {
            return ValidateChess(itemsControl, e, (ChessPieceOnBoard)sourceContext, (ChessPuzzleEditorViewModel)targetContext, false);
        }

        return false;
    }

    public override bool Execute(object sender, DragEventArgs e, object sourceContext, object targetContext,
        object state)
    {
        if (e.Source is Control && sender is ItemsControl itemsControl)
        {
            return ValidateChess(itemsControl, e, (ChessPieceOnBoard)sourceContext, (ChessPuzzleEditorViewModel)targetContext, true);
        }

        return false;
    }
}
