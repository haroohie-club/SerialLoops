using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ScreenShakeScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private short _duration;
    public short Duration
    {
        get => _duration;
        set
        {
            this.RaiseAndSetIfChanged(ref _duration, value);
            ((ShortScriptParameter)Command.Parameters[0]).Value = _duration;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _duration;
            Script.UnsavedChanges = true;
        }
    }

    private short _horizontalIntensity;
    public short HorizontalIntensity
    {
        get => _horizontalIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref _horizontalIntensity, value);
            ((ShortScriptParameter)Command.Parameters[1]).Value = _horizontalIntensity;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = _horizontalIntensity;
            Script.UnsavedChanges = true;
        }
    }

    private short _verticalIntensity;
    public short VerticalIntensity
    {
        get => _verticalIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref _verticalIntensity, value);
            ((ShortScriptParameter)Command.Parameters[2]).Value = _verticalIntensity;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = _verticalIntensity;
            Script.UnsavedChanges = true;
        }
    }

    public ScreenShakeScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) :
        base(command, scriptEditor, log)
    {
        _duration = ((ShortScriptParameter)Command.Parameters[0]).Value;
        _horizontalIntensity = ((ShortScriptParameter)Command.Parameters[1]).Value;
        _verticalIntensity = ((ShortScriptParameter)Command.Parameters[2]).Value;
    }
}
