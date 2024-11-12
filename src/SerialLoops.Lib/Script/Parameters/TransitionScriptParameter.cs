using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class TransitionScriptParameter(string name, short transition) : ScriptParameter(name, ParameterType.TRANSITION)
{
    public TransitionEffect Transition { get; set; } = (TransitionEffect)transition;
    public override short[] GetValues(object? obj = null) => [(short)Transition];

    public override TransitionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Transition);
    }

    public enum TransitionEffect
    {
        WIPE_RIGHT = 0,
        WIPE_DOWN = 1,
        WIPE_DIAGONAL_RIGHT_DOWN = 2,
        BLINDS = 3,
        BLINDS2 = 4,
        WIPE_LEFT = 5,
        WIPE_UP = 6,
        WIPE_DIAGONAL_LEFT_UP = 7,
    }
}
