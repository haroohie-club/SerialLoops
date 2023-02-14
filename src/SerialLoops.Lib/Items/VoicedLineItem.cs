using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio;
using System.IO;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class VoicedLineItem : Item
    {
        public string VoiceFile { get; set; }
        public int Index { get; set; }
        public AdxEncoding AdxType { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public VoicedLineItem(string voiceFile, int index, Project project) : base(Path.GetFileNameWithoutExtension(voiceFile), ItemType.Voice)
        {
            VoiceFile = voiceFile;
            Index = index;
            PopulateScriptUses(project);
        }

        public override void Refresh(Project project)
        {
            PopulateScriptUses(project);
        }

        public void PopulateScriptUses(Project project)
        {
            var list = project.Evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.DIALOGUE.ToString()).Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[5] == Index).ToList();
            list.AddRange(project.Evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.VCE_PLAY.ToString()).Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[0] == Index));

            ScriptUses = list.ToArray();
        }
    }
}
