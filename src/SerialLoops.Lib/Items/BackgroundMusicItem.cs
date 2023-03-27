using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Lib.Util;
using System;
using System.IO;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class BackgroundMusicItem : Item, ISoundItem
    {
        private string _bgmFile;

        public string BgmFile { get; set; }
        public int Index { get; set; }
        public string BgmName { get; set; }
        public string ExtrasShort { get; set; }
        public string CachedWaveFile { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public BackgroundMusicItem(string bgmFile, int index, ExtraFile extras, Project project) : base(Path.GetFileNameWithoutExtension(bgmFile), ItemType.BGM)
        {
            BgmFile = Path.GetRelativePath(project.IterativeDirectory, bgmFile);
            _bgmFile = bgmFile;
            Index = index;
            BgmName = extras.Bgms.FirstOrDefault(b => b.Index == Index).Name?.GetSubstitutedString(project) ?? "";
            DisplayName = string.IsNullOrEmpty(BgmName) ? Name : BgmName;
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

        public void Replace(string wavFile, string baseDirectory, string iterativeDirectory, string bgmCachedFile)
        {
            File.Copy(wavFile, bgmCachedFile, true);
            long endTime = 0;
            using (WaveFileReader wav = new(bgmCachedFile))
            {
                endTime = (uint)(wav.SampleCount / wav.WaveFormat.SampleRate);
            }
            AdxUtil.EncodeWav(wavFile, Path.Combine(baseDirectory, BgmFile), 0.0, endTime);
            File.Copy(Path.Combine(baseDirectory, BgmFile), Path.Combine(iterativeDirectory, BgmFile), true);
        }
        
        public IWaveProvider GetWaveProvider(ILogger log)
        {
            byte[] adxBytes = Array.Empty<byte>();
            try
            {
                adxBytes = File.ReadAllBytes(_bgmFile);
            }
            catch
            {
                if (!File.Exists(_bgmFile))
                {
                    log.LogError($"Failed to load BGM file {_bgmFile}: file not found.");
                }
                else
                {
                    log.LogError($"Failed to load BGM file {_bgmFile}: file invalid.");
                }
            }

            return new AdxWaveProvider(new AdxDecoder(adxBytes, log));
        }
    }
}
