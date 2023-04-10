using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items
{
    public class DialogueConfigItem : Item
    {
        public MessageInfo DialogueConfig { get; set; }

        public DialogueConfigItem(MessageInfo messageInfo) : base($"DIALOGUE_{messageInfo.Character}", ItemType.Dialogue_Config)
        {
            DialogueConfig = messageInfo;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }
    }
}
