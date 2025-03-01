using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using HaruhiChokuretsuLib.Archive.Data;
using SerialLoops.ViewModels.Editors;

namespace SerialLoops.Views.Editors;

public partial class ChessPuzzleEditorView : UserControl
{
    public ChessPuzzleEditorView()
    {
        InitializeComponent();
    }

    private void ChessPiece_OnPointerEntered(object sender, PointerEventArgs e)
    {
        Panel pieceBg = sender as Panel;
        if (((ChessPieceOnBoard)pieceBg?.DataContext)?.Piece == ChessFile.ChessPiece.Empty)
        {
            return;
        }
        pieceBg!.Background = Brushes.Gold;
    }

    private void ChessPiece_OnPointerExited(object sender, PointerEventArgs e)
    {
        Panel pieceBg = sender as Panel;
        if (((ChessPieceOnBoard)pieceBg?.DataContext)?.Piece == ChessFile.ChessPiece.Empty)
        {
            return;
        }
        pieceBg!.Background = Brushes.Transparent;
    }
}

