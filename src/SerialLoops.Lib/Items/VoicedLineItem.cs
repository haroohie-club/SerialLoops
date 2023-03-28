using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class VoicedLineItem : Item, ISoundItem
    {
        private string _vceFile;

        public string VoiceFile { get; set; }
        public int Index { get; set; }
        public AdxEncoding AdxType { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public VoicedLineItem(string voiceFile, int index, Project project) : base(Path.GetFileNameWithoutExtension(voiceFile), ItemType.Voice)
        {
            VoiceFile = Path.GetRelativePath(project.IterativeDirectory, voiceFile);
            _vceFile = voiceFile;
            Index = index;
            PopulateScriptUses(project);
        }
        
        public IWaveProvider GetWaveProvider(ILogger log, bool loop = false)
        {
            byte[] adxBytes = Array.Empty<byte>();
            try
            {
                adxBytes = File.ReadAllBytes(_vceFile);
            }
            catch
            {
                if (!File.Exists(_vceFile))
                {
                    log.LogError($"Failed to load voice file {_vceFile}: file not found.");
                }
                else
                {
                    log.LogError($"Failed to load voice file {_vceFile}: file invalid.");
                }
            }

            AdxType = (AdxEncoding)adxBytes[4];

            IAdxDecoder decoder;
            if (AdxType == AdxEncoding.Ahx10 || AdxType == AdxEncoding.Ahx11)
            {
                decoder = new AhxDecoder(adxBytes, log);
            }
            else
            {
                decoder = new AdxDecoder(adxBytes, log);
            }

            return new AdxWaveProvider(decoder);
        }

        public void Replace(string wavFile, string baseDirectory, string iterativeDirectory)
        {
            AdxUtil.EncodeWav(wavFile, Path.Combine(baseDirectory, VoiceFile), true);
            File.Copy(Path.Combine(baseDirectory, VoiceFile), Path.Combine(iterativeDirectory, VoiceFile), true);
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
