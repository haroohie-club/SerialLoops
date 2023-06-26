using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio.SDAT.SoundArchiveComponents;
using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class SfxItem : Item
    {
        public short Index { get; set; }
        public SfxEntry Entry { get; set; }
        public string AssociatedBank { get; private set; }
        public List<string> AssociatedGroups { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public SfxItem(SfxEntry entry, string name, short index, Project project) : base(name, ItemType.SFX)
        {
            Entry = entry;
            Index = index;
            AssociatedBank = project.Snd.SequenceArchives[entry.SequenceArchive].File.Sequences[entry.Index].Bank.Name;
            AssociatedGroups = project.Snd.Groups.Where(g => g.Entries.Any(e => e.LoadBank && ((BankInfo)e.Entry).Name.Equals(AssociatedBank))).Select(g => g.Name).ToList();
            PopulateScriptUses(project);
        }

        public override void Refresh(Project project, ILogger log)
        {
            PopulateScriptUses(project);
        }

        public void PopulateScriptUses(Project project)
        {
            ScriptUses = project.Evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.SND_PLAY.ToString()).Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[0] == Index).ToArray();
        }
    }
}
