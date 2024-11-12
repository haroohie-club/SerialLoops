using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class SpriteShakeScriptParameter(string name, short shakeEffect)
    : ScriptParameter(name, ParameterType.SPRITE_SHAKE)
{
    public SpriteShakeEffect ShakeEffect { get; set; } = (SpriteShakeEffect)shakeEffect;
    public override short[] GetValues(object? obj = null) => [(short)ShakeEffect];

    public override SpriteShakeScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)ShakeEffect);
    }

    public enum SpriteShakeEffect : short
    {
        NONE = 0,
        SHAKE_CENTER = 1,
        BOUNCE_HORIZONTAL_CENTER = 2,
        BOUNCE_HORIZONTAL_CENTER_WITH_SMALL_SHAKES = 3,
        SHAKE_RIGHT = 4,
        SHAKE_LEFT = 5,
    }
}
