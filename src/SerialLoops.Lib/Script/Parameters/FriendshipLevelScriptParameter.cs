using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class FriendshipLevelScriptParameter(string name, short character)
    : ScriptParameter(name, ParameterType.FRIENDSHIP_LEVEL)
{
    public enum FriendshipCharacter : short
    {
        Haruhi = 2,
        Mikuru = 3,
        Nagato = 4,
        Koizumi = 5,
        // 6th slot is unreferenceable in the game itself
        Tsuruya = 7,
    }

    public FriendshipCharacter Character { get; set; } = (FriendshipCharacter)character;

    public override short[] GetValues(object? obj = null) => [(short)Character];

    public override FriendshipLevelScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Character);
    }
}
