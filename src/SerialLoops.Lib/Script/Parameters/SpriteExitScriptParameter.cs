using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class SpriteExitScriptParameter : ScriptParameter
{
    public SpriteExitTransition ExitTransition { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { (short)ExitTransition };

    public SpriteExitScriptParameter(string name, short exitTransition) : base(name, ParameterType.SPRITE_EXIT)
    {
        ExitTransition = (SpriteExitTransition)exitTransition;
    }

    public override SpriteExitScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)ExitTransition);
    }

    public enum SpriteExitTransition : short
    {
        NO_EXIT = 0,
        SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT = 1,
        SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT = 2,
        SLIDE_CENTER_TO_RIGHT_AND_STAY = 3,
        SLIDE_CENTER_TO_LEFT_AND_STAY = 4,
        SLIDE_LEFT_TO_RIGHT_FADE_OUT = 5,
        SLIDE_LEFT_TO_LEFT_FADE_OUT = 6,
        FADE_OUT_CENTER = 7,
        FADE_OUT_LEFT = 8,
        SLIDE_LEFT_TO_RIGHT_AND_STAY = 9,
        SLIDE_RIGHT_TO_LEFT_AND_STAY = 10,
    }
}