using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items
{
    public class SfxItem : Item
    {
        public int Index { get; set; }
        public SfxEntry Entry { get; set; }

        public SfxItem(SfxEntry entry, string name, int index) : base(name, ItemType.SFX)
        {
            Entry = entry;
            Index = index;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }
    }
}
