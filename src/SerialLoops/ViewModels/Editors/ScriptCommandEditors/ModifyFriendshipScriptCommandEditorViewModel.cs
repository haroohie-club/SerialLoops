using System;
using System.Collections.ObjectModel;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ModifyFriendshipScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public ObservableCollection<string> Characters { get; } = new(Enum.GetNames<FriendshipLevelScriptParameter.FriendshipCharacter>());

    public string Character
    {
        get => ((FriendshipLevelScriptParameter)Command.Parameters[0]).Character.ToString();
        set
        {
            FriendshipLevelScriptParameter.FriendshipCharacter character =
                Enum.Parse<FriendshipLevelScriptParameter.FriendshipCharacter>(value);
            ((FriendshipLevelScriptParameter)Command.Parameters[0]).Character = character;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)character;
            this.RaisePropertyChanged();
            Script.UnsavedChanges = true;
        }
    }

    public short ModifiedAmount
    {
        get => ((ShortScriptParameter)Command.Parameters[1]).Value;
        set
        {
            ((ShortScriptParameter)Command.Parameters[1]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = value;
            this.RaisePropertyChanged();
            Script.UnsavedChanges = true;
        }
    }
}
