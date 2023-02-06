﻿namespace SerialLoops.Lib.Script
{
    public class SpriteShakeScriptParameter : ScriptParameter
    {
        public SpriteShakeEffect ShakeEffect { get; set; }

        public SpriteShakeScriptParameter(string name, short shakeEffect) : base(name, ParameterType.SPRITE_SHAKE)
        {
            ShakeEffect = (SpriteShakeEffect)shakeEffect;
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
}
