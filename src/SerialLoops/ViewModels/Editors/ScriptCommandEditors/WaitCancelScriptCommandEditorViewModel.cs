using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class WaitCancelScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public short WaitTime
    {
        get => ((ShortScriptParameter)Command.Parameters[0]).Value;
        set
        {
            ((ShortScriptParameter)Command.Parameters[0]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = value;
            this.RaisePropertyChanged();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }
}
