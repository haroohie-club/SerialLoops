using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class SpritePostTransitionScriptParameter : ScriptParameter
{
    public SpritePostTransition PostTransition { get; set; }
    public override short[] GetValues(object obj = null) => [(short)PostTransition];

    public override string GetValueString(Project project)
    {
        return project.Localize(PostTransition.ToString());
    }

    public SpritePostTransitionScriptParameter(string name, short exitTransition) : base(name, ParameterType.SPRITE_EXIT)
    {
        PostTransition = (SpritePostTransition)exitTransition;
    }

    public override SpritePostTransitionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)PostTransition);
    }

    public static SpritePostTransition[] ExitTransitions { get; } =
    [
        SpritePostTransition.SLIDE_LEFT_FADE_OUT,
        SpritePostTransition.SLIDE_RIGHT_FADE_OUT,
        SpritePostTransition.SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT,
        SpritePostTransition.SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT,
        SpritePostTransition.FADE_OUT_CENTER,
        SpritePostTransition.FADE_OUT_LEFT,
    ];

    public enum SpritePostTransition : short
    {
        NO_EXIT = 0,
        SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT = 1,
        SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT = 2,
        SLIDE_CENTER_TO_RIGHT_AND_STAY = 3,
        SLIDE_CENTER_TO_LEFT_AND_STAY = 4,
        SLIDE_RIGHT_FADE_OUT = 5,
        SLIDE_LEFT_FADE_OUT = 6,
        FADE_OUT_CENTER = 7,
        FADE_OUT_LEFT = 8,
        SLIDE_LEFT_TO_RIGHT_AND_STAY = 9,
        SLIDE_RIGHT_TO_LEFT_AND_STAY = 10,
    }
}
