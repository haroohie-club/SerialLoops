using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class FriendshipLevelScriptParameter : ScriptParameter
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

    public FriendshipCharacter Character { get; set; }

    public override short[] GetValues(object obj = null) => new short[] { (short)Character };

    public FriendshipLevelScriptParameter(string name, short character) : base(name, ParameterType.FRIENDSHIP_LEVEL)
    {
        Character = (FriendshipCharacter)character;
    }

    public override FriendshipLevelScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Character);
    }
}