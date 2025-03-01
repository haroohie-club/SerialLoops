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

    private void WhiteBoard_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        ((ChessMoveScriptCommandEditorViewModel)DataContext)!.Board1Click(e.GetPosition((Image)sender));
    }

    private void BlackBoard_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        ((ChessMoveScriptCommandEditorViewModel)DataContext)!.Board2Click(e.GetPosition((Image)sender));
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ChessMoveScriptCommandEditorViewModel)DataContext)!.LoadChessboard();
    }
}

