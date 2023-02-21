using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using System;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace SerialLoops.Lib.Items
{
    public class BackgroundMusicItem : Item, ISoundItem
    {
        public string BgmFile { get; set; }
        public int Index { get; set; }
        public string BgmName { get; set; }
        public string ExtrasShort { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public BackgroundMusicItem(string bgmFile, int index, ExtraFile extras, Project project) : base(Path.GetFileNameWithoutExtension(bgmFile), ItemType.BGM)
        {
            BgmFile = bgmFile;
            Index = index;
            BgmName = extras.Bgms.FirstOrDefault(b => b.Index == Index).Name ?? "";
            PopulateScriptUses(project);
        }

        public override void Refresh(Project project)
        {
            PopulateScriptUses(project);
        }

        public void PopulateScriptUses(Project project)
        {
            ScriptUses = project.Evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.BGM_PLAY.ToString()).Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[0] == Index).ToArray();
        }
        
        public IWaveProvider GetWaveProvider(ILogger log)
        {
            byte[] adxBytes = Array.Empty<byte>();
            try
            {
                adxBytes = File.ReadAllBytes(BgmFile);
            }
            catch
            {
                if (!File.Exists(BgmFile))
                {
                    log.LogError($"Failed to load BGM file {BgmFile}: file not found.");
                }
                else
                {
                    log.LogError($"Failed to load BGM file {BgmFile}: file invalid.");
                }
            }

            return new AdxWaveProvider(new AdxDecoder(adxBytes, log));
        }
    }
}
