using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class SpriteScriptParameter : ScriptParameter
    {
        public CharacterSpriteItem Sprite { get; set; }

        public SpriteScriptParameter(string name, CharacterSpriteItem sprite) : base(name, ParameterType.SPRITE)
        {
            Sprite = sprite;
        }

        public override SpriteScriptParameter Clone()
        {
            return new(Name, Sprite);
        }
    }
}
