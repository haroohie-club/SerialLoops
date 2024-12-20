using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class TransInOutScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public ObservableCollection<TransitionLocalized> Transitions { get; }
        = new(Enum.GetValues<TransitionScriptParameter.TransitionEffect>().Select(t => new TransitionLocalized(t)));
    private TransitionLocalized _transition;
    public TransitionLocalized Transition
    {
        get => _transition;
        set
        {
            this.RaiseAndSetIfChanged(ref _transition, value);
            ((TransitionScriptParameter)Command.Parameters[0]).Transition = _transition.Transition;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_transition.Transition;
            Script.UnsavedChanges = true;
        }
    }

    public TransInOutScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        _transition = new(((TransitionScriptParameter)Command.Parameters[0]).Transition);
    }
}

public readonly struct TransitionLocalized(TransitionScriptParameter.TransitionEffect transition)
{
    public TransitionScriptParameter.TransitionEffect Transition { get; } = transition;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(transition.ToString());
}
