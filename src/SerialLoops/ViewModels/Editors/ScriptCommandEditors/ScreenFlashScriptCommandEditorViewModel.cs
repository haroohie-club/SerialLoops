using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ScreenFlashScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor) : ScriptCommandEditorViewModel(command, scriptEditor)
{
    private short _fadeInTime = ((ShortScriptParameter)command.Parameters[0]).Value;
    public short FadeInTime
    {
        get => _fadeInTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _fadeInTime, value);
            ((ShortScriptParameter)Command.Parameters[0]).Value = _fadeInTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _fadeInTime;
            Script.UnsavedChanges = true;
        }
    }

    private short _holdTime = ((ShortScriptParameter)command.Parameters[1]).Value;
    public short HoldTime
    {
        get => _holdTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _holdTime, value);
            ((ShortScriptParameter)Command.Parameters[1]).Value = _holdTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = _holdTime;
            Script.UnsavedChanges = true;
        }
    }

    private short _fadeOutTime = ((ShortScriptParameter)command.Parameters[2]).Value;
    public short FadeOutTime
    {
        get => _fadeOutTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _fadeOutTime, value);
            ((ShortScriptParameter)Command.Parameters[2]).Value = _fadeOutTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = _fadeOutTime;
            Script.UnsavedChanges = true;
        }
    }

    private SKColor _color = ((ColorScriptParameter)command.Parameters[3]).Color;
    public SKColor Color
    {
        get => _color;
        set
        {
            this.RaiseAndSetIfChanged(ref _color, value);
            ((ColorScriptParameter)Command.Parameters[3]).Color = _color;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[3] = _color.Red;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[4] = _color.Green;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[5] = _color.Blue;
            Script.UnsavedChanges = true;
        }
    }
}