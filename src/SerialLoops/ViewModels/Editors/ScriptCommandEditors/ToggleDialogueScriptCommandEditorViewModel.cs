using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ToggleDialogueScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    private bool _dialogueVisible = ((BoolScriptParameter)command.Parameters[0]).Value;
    public bool DialogueVisible
    {
        get => _dialogueVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _dialogueVisible, value);
            ((BoolScriptParameter)Command.Parameters[0]).Value = _dialogueVisible;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _dialogueVisible
                ? ((BoolScriptParameter)Command.Parameters[0]).TrueValue
                : ((BoolScriptParameter)Command.Parameters[0]).FalseValue;
            Script.UnsavedChanges = true;
            ScriptEditor.UpdatePreview();
        }
    }
}
