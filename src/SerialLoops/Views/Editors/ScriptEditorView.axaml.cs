using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using SerialLoops.Models;
using SerialLoops.ViewModels.Editors;

namespace SerialLoops.Views.Editors;

public partial class ScriptEditorView : UserControl
{
    private ITreeItem _dragSource = null;

    public ScriptEditorView()
    {
        InitializeComponent();
    }

    private void TreeDataGrid_OnRowDragStarted(object sender, TreeDataGridRowDragStartedEventArgs e)
    {
        if (!((ScriptEditorViewModel)DataContext!).CanDrag((ITreeItem)e.Models.ElementAt(0)))
        {
            e.AllowedEffects = DragDropEffects.None;
        }
        else
        {
            e.AllowedEffects = DragDropEffects.Move;
            _dragSource = (ITreeItem)e.Models.ElementAt(0);
        }
    }

    private void TreeDataGrid_OnRowDrop(object sender, TreeDataGridRowDragEventArgs e)
    {
        ((ScriptEditorViewModel)DataContext!).HandleDragDrop(_dragSource, (ITreeItem)e.TargetRow.Model, e.Position);
        _dragSource = null;
        e.Inner.DragEffects = DragDropEffects.None;
        e.Handled = true;
    }
}
