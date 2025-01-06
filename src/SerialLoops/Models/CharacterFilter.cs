using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;

namespace SerialLoops.Models;

public class CharacterFilter(CharacterItem character)
{
    public CharacterItem Character { get; set; } = character;

    public string DisplayName =>
        (Character?.MessageInfo.Character == Speaker.UNKNOWN ? Strings.Any_Character : Character?.DisplayName[4..]) ??
        Strings.No_Character;
}
