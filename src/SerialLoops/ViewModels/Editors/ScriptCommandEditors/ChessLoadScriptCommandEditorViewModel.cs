using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChessLoadScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<ChessPuzzleItem> ChessPuzzles { get; }
    private ChessPuzzleItem _chessPuzzle;
    public ChessPuzzleItem ChessPuzzle
    {
        get => _chessPuzzle;
        set
        {
            this.RaiseAndSetIfChanged(ref _chessPuzzle, value);
            ((ChessPuzzleScriptParameter)Command.Parameters[0]).ChessPuzzle = _chessPuzzle;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_chessPuzzle.ChessPuzzle.Index;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ChessLoadScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, MainWindowViewModel window, ILogger log)
        : base(command, scriptEditor, log)
    {
        ChessPuzzles = new(window.OpenProject.Items.Where(c => c.Type == ItemDescription.ItemType.Chess_Puzzle).Cast<ChessPuzzleItem>());
        _chessPuzzle = ((ChessPuzzleScriptParameter)command.Parameters[0]).ChessPuzzle;
        Tabs = window.EditorTabs;
    }
}
