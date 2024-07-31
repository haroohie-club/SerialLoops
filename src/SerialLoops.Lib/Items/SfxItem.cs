using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Audio.SDAT.SoundArchiveComponents;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items
{
    public class SfxItem : Item
    {
        public short Index { get; set; }
        public SfxEntry Entry { get; set; }
        public string AssociatedBank { get; private set; }
        public List<string> AssociatedGroups { get; set; }

        public SfxItem(SfxEntry entry, string name, short index, Project project) : base(name, ItemType.SFX)
        {
            Entry = entry;
            Index = index;
            AssociatedBank = project.Snd.SequenceArchives[entry.SequenceArchive].File.Sequences[entry.Index].Bank.Name;
            AssociatedGroups = project.Snd.Groups.Where(g => g.Entries.Any(e => e.LoadBank && ((BankInfo)e.Entry).Name.Equals(AssociatedBank))).Select(g => g.Name).ToList();
        }

        public override void Refresh(Project project, ILogger log)
        {
        }
    }
}
