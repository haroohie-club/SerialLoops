using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class SpriteScriptParameter(string name, CharacterSpriteItem? sprite)
    : ScriptParameter(name, ParameterType.SPRITE)
{
    public CharacterSpriteItem? Sprite { get; set; } = sprite;
    public override short[] GetValues(object? obj = null) => [(short)(Sprite?.Index ?? 0)];

    public override SpriteScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Sprite);
    }
}
