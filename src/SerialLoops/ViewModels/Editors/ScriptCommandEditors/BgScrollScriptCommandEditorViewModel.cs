using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class BgScrollScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public ObservableCollection<LocalizedBgScrollDirection> ScrollDirections { get; } = new(Enum
        .GetValues<BgScrollDirectionScriptParameter.BgScrollDirection>()
        .Select(s => new LocalizedBgScrollDirection(s)));

    private LocalizedBgScrollDirection _scrollDirection = new(((BgScrollDirectionScriptParameter)command.Parameters[0]).ScrollDirection);
    public LocalizedBgScrollDirection ScrollDirection
    {
        get => _scrollDirection;
        set
        {
            this.RaiseAndSetIfChanged(ref _scrollDirection, value);
            ((BgScrollDirectionScriptParameter)Command.Parameters[0]).ScrollDirection = _scrollDirection.Direction;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_scrollDirection.Direction;
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public short ScrollSpeed
    {
        get => ((ShortScriptParameter)Command.Parameters[1]).Value;
        set
        {
            ((ShortScriptParameter)Command.Parameters[1]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = value;
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }
}

public class LocalizedBgScrollDirection(BgScrollDirectionScriptParameter.BgScrollDirection direction)
{
    public BgScrollDirectionScriptParameter.BgScrollDirection Direction { get; } = direction;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(direction.ToString());

    public override bool Equals(object obj)
    {
        return Direction == ((LocalizedBgScrollDirection)obj)?.Direction;
    }

    public override int GetHashCode()
    {
        return Direction.GetHashCode();
    }
}
