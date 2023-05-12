using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items
{
    public class CharacterItem : Item
    {
        public MessageInfo Character { get; set; }

        public CharacterItem(MessageInfo character) : base($"CHR_{character.Character}", ItemType.Character)
        {
            Character = character;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }
    }
}
