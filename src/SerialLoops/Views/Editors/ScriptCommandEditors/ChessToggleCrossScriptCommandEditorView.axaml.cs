using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Editors.ScriptCommandEditors;

namespace SerialLoops.Views.Editors.ScriptCommandEditors;

public partial class ChessToggleCrossScriptCommandEditorView : UserControl
{
    public ChessToggleCrossScriptCommandEditorView()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ChessToggleCrossScriptCommandEditorViewModel)DataContext)!.LoadChessboard();
    }

    private void Chessboard_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        ((ChessToggleCrossScriptCommandEditorViewModel)DataContext)!.BoardClick(e.GetPosition((Canvas)sender));
    }
}

