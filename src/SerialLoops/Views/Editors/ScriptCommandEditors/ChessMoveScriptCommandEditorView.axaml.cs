using Avalonia.Controls;
using Avalonia.Input;
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
        ((ChessMoveScriptCommandEditorViewModel)DataContext)!.WhiteBoardClick(e.GetPosition((Image)sender));
    }

    private void BlackBoard_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        ((ChessMoveScriptCommandEditorViewModel)DataContext)!.BlackBoardClick(e.GetPosition((Image)sender));
    }
}

