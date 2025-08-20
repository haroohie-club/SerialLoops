using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;

namespace SerialLoops.Models;

public class CharacterFilter(CharacterItem character)
{
    public CharacterItem Character { get; set; } = character;

    public override string ToString() =>
        (Character?.MessageInfo.Character == Speaker.UNKNOWN ? Strings.CharacterFilterAnyCharacter : Character?.DisplayName[4..]) ??
        Strings.CharacterEditorNoCharacter;
}
