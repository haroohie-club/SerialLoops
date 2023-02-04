﻿using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script
{
    public class SpriteScriptParameter : ScriptParameter
    {
        public CharacterSpriteItem Sprite { get; set; }

        public SpriteScriptParameter(string name, CharacterSpriteItem sprite) : base(name, ParameterType.SPRITE)
        {
            Sprite = sprite;
        }
    }
}
