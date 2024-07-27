using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class SpriteScriptParameter : ScriptParameter
    {
        public CharacterSpriteItem Sprite { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)(Sprite?.Index ?? 0) };

        public SpriteScriptParameter(string name, CharacterSpriteItem sprite) : base(name, ParameterType.SPRITE)
        {
            Sprite = sprite;
        }

        public override SpriteScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Sprite);
        }
    }
}
