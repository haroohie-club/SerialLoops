using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors
{
    public class WaitScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor) : ScriptCommandEditorViewModel(command, scriptEditor)
    {
        private short _waitTime = ((ShortScriptParameter)command.Parameters[0]).Value;
        public short WaitTime
        {
            get => _waitTime;
            set
            {
                this.RaiseAndSetIfChanged(ref _waitTime, value);
                ((ShortScriptParameter)Command.Parameters[0]).Value = _waitTime;
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[0] = _waitTime;
                Script.UnsavedChanges = true;
            }
        }
    }
}
