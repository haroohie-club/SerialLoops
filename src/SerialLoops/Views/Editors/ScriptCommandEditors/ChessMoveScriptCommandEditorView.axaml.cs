using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Editors.ScriptCommandEditors;

namespace SerialLoops.Views.Editors.ScriptCommandEditors;

public partial class ChessMoveScriptCommandEditorView : UserControl
{
    public ChessMoveScriptCommandEditorView()
    {
        InitializeComponent();
    }

    private void Board1_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        ((ChessMoveScriptCommandEditorViewModel)DataContext)!.Board1Click(e.GetPosition((Canvas)sender));
    }

    private void Board2_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        ((ChessMoveScriptCommandEditorViewModel)DataContext)!.Board2Click(e.GetPosition((Canvas)sender));
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ChessMoveScriptCommandEditorViewModel)DataContext)!.LoadChessboard();
    }
}

