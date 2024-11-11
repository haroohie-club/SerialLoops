using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class FlagScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    private short _flagId = ((FlagScriptParameter)command.Parameters[0]).Id;
    public string Flag
    {
        get => ((FlagScriptParameter)Command.Parameters[0]).FlagName;
        set
        {
            if (short.TryParse(value[1..], out short flag))
            {
                if (value[0] == 'G')
                {
                    flag += 100;
                }
                this.RaiseAndSetIfChanged(ref _flagId, flag);
                ((FlagScriptParameter)Command.Parameters[0]).Id = _flagId;
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[0] = _flagId;
                Script.UnsavedChanges = true;
            }
        }
    }

    private bool _setClear = ((BoolScriptParameter)command.Parameters[1]).Value;
    public bool SetClear
    {
        get => _setClear;
        set
        {
            this.RaiseAndSetIfChanged(ref _setClear, value);
            ((BoolScriptParameter)Command.Parameters[1]).Value = _setClear;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = _setClear
                ? ((BoolScriptParameter)Command.Parameters[1]).TrueValue
                : ((BoolScriptParameter)Command.Parameters[1]).FalseValue;
            Script.UnsavedChanges = true;
        }
    }
}
