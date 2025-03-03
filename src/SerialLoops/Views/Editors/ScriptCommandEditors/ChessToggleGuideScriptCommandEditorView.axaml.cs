using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Editors.ScriptCommandEditors;

namespace SerialLoops.Views.Editors.ScriptCommandEditors;

public partial class ChessToggleGuideScriptCommandEditorView : UserControl
{
    public ChessToggleGuideScriptCommandEditorView()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ChessToggleGuideScriptCommandEditorViewModel)DataContext)!.LoadChessboard();
    }

    private void Chessboard_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        ((ChessToggleGuideScriptCommandEditorViewModel)DataContext)!.BoardClick(e.GetPosition((Canvas)sender));
    }
}

