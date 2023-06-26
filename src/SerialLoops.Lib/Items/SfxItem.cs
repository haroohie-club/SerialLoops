using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;

namespace SerialLoops.Lib.Items
{
    public class SfxItem : Item
    {
        public SfxEntry Entry { get; set; }

        public SfxItem(SfxEntry entry, string name) : base(name, ItemType.SFX)
        {
            Entry = entry;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }
    }
}
