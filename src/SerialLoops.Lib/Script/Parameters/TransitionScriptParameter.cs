using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class TransitionScriptParameter : ScriptParameter
{
    public TransitionEffect Transition { get; set; }
    public override short[] GetValues(object obj = null) => [(short)Transition];

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
        TransitionWipeRight = 0,
        TransitionWipeDown = 1,
        TransitionWipeDiagonalRightDown = 2,
        TransitionBlinds = 3,
        TransitionBlinds2 = 4,
        TransitionWipeLeft = 5,
        TransitionWipeUp = 6,
        TransitionWipeDiagonalLeftUp = 7,
    }
}
