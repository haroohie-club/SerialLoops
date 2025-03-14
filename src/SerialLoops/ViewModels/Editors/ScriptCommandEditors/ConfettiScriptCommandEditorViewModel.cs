using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ConfettiScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public bool Visible
    {
        get => ((BoolScriptParameter)Command.Parameters[0]).Value;
        set
        {
            ((BoolScriptParameter)Command.Parameters[0]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = value ? ((BoolScriptParameter)Command.Parameters[0]).TrueValue : ((BoolScriptParameter)Command.Parameters[0]).FalseValue;
            this.RaisePropertyChanged();
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }
}
