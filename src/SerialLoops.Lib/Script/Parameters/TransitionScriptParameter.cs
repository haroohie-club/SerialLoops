using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class TransitionScriptParameter : ScriptParameter
{
    public TransitionEffect Transition { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { (short)Transition };

    public override string GetValueString(Project project)
    {
        return project.Localize(Transition.ToString());
    }

    public TransitionScriptParameter(string name, short transition) : base(name, ParameterType.TRANSITION)
    {
        Transition = (TransitionEffect)transition;
    }

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
