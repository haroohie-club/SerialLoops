using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Editors.ScriptCommandEditors;

namespace SerialLoops.Views.Editors.ScriptCommandEditors;

public partial class ChessToggleHighlightScriptCommandEditorView : UserControl
{
    public ChessToggleHighlightScriptCommandEditorView()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ChessToggleHighlightScriptCommandEditorViewModel)DataContext)!.LoadChessboard();
    }

    private void Chessboard_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        ((ChessToggleHighlightScriptCommandEditorViewModel)DataContext)!.BoardClick(e.GetPosition((Canvas)sender));
    }
}

