﻿using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class SpriteShakeScriptParameter : ScriptParameter
{
    public SpriteShakeEffect ShakeEffect { get; set; }
    public override short[] GetValues(object obj = null) => [(short)ShakeEffect];

    public override string GetValueString(Project project)
    {
        return project.Localize(ShakeEffect.ToString());
    }

    public SpriteShakeScriptParameter(string name, short shakeEffect) : base(name, ParameterType.SPRITE_SHAKE)
    {
        ShakeEffect = (SpriteShakeEffect)shakeEffect;
    }

    public override SpriteShakeScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)ShakeEffect);
    }

    public enum SpriteShakeEffect : short
    {
        NO_SHAKE = 0,
        SHAKE_CENTER = 1,
        BOUNCE_HORIZONTAL_CENTER = 2,
        BOUNCE_HORIZONTAL_CENTER_WITH_SMALL_SHAKES = 3,
        SHAKE_RIGHT = 4,
        SHAKE_LEFT = 5,
    }
}
