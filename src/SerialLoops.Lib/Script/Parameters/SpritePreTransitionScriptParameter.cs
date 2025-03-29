using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class SpritePreTransitionScriptParameter : ScriptParameter
{
    public SpritePreTransition PreTransition { get; set; }
    public override short[] GetValues(object obj = null) => [(short)PreTransition];

    public override string GetValueString(Project project)
    {
        return project.Localize(PreTransition.ToString());
    }

    public SpritePreTransitionScriptParameter(string name, short entranceTransition) : base(name, ParameterType.SPRITE_ENTRANCE)
    {
        PreTransition = (SpritePreTransition)entranceTransition;
    }

    public override SpritePreTransitionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)PreTransition);
    }

    public enum SpritePreTransition : short
    {
        NO_TRANSITION = 0,
        SLIDE_LEFT_TO_CENTER = 1,
        SLIDE_RIGHT_TO_CENTER = 2,
        SLIDE_LEFT_TO_RIGHT = 3,
        SLIDE_RIGHT_TO_LEFT = 4,
        BOUNCE_LEFT_TO_RIGHT = 5,
        BOUNCE_RIGHT_TO_LEFT = 6,
        FADE_TO_CENTER = 7,
        SLIDE_LEFT = 8,
        SLIDE_RIGHT = 9,
        PEEK_RIGHT_TO_LEFT = 10,
        FADE_IN_LEFT = 11,
    }
}
