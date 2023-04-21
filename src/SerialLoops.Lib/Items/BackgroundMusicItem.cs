using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using NAudio.Flac;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
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

        public BackgroundMusicItem(string bgmFile, int index, Project project) : base(Path.GetFileNameWithoutExtension(bgmFile), ItemType.BGM)
        {
            BgmFile = Path.GetRelativePath(project.IterativeDirectory, bgmFile);
            _bgmFile = bgmFile;
            Index = index;
            BgmName = project.Extra.Bgms.FirstOrDefault(b => b.Index == Index)?.Name?.GetSubstitutedString(project) ?? "";
            DisplayName = string.IsNullOrEmpty(BgmName) ? Name : BgmName;
            CanRename = string.IsNullOrEmpty(BgmName);
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
                    sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.BGM_PLAY.ToString()).Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[0] == Index).ToArray();
        }

        public void Replace(string audioFile, string baseDirectory, string iterativeDirectory, string bgmCachedFile, bool loopEnabled, uint loopStartSample, uint loopEndSample, ILogger log)
        {
            // The MP3 reader is able to create wave files but for whatever reason messes with the ADX encoder
            // So we just convert to WAV AOT
            if (Path.GetExtension(audioFile).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                log.Log($"Converting {audioFile} to WAV...");
                using Mp3FileReaderBase mp3Reader = new(audioFile, new Mp3FileReaderBase.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf)));
                WaveFileWriter.CreateWaveFile(bgmCachedFile, mp3Reader.ToSampleProvider().ToWaveProvider16());
                audioFile = bgmCachedFile;
            }
            // Ditto the Vorbis decoder
            else if (Path.GetExtension(audioFile).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                log.Log($"Converting {audioFile} to WAV...");
                using VorbisWaveReader vorbisReader = new(audioFile);
                WaveFileWriter.CreateWaveFile(bgmCachedFile, vorbisReader.ToSampleProvider().ToWaveProvider16());
                audioFile = bgmCachedFile;
            }
            using WaveStream audio = Path.GetExtension(audioFile).ToLower() switch
            {
                ".wav" => new WaveFileReader(audioFile),
                ".flac" => new FlacReader(audioFile),
                _ => null,
            };
            if (audio is null)
            {
                log.LogError($"Invalid audio file '{audioFile}' selected.");
                return;
            }
            if (audio.WaveFormat.SampleRate > SoundItem.MAX_SAMPLERATE)
            {
                log.Log($"Downsampling audio from {audio.WaveFormat.SampleRate} to NDS max sample rate {SoundItem.MAX_SAMPLERATE}...");
                string newAudioFile = Path.Combine(Path.GetDirectoryName(bgmCachedFile), $"{Path.GetFileNameWithoutExtension(bgmCachedFile)}-downsampled.wav");
                WaveFileWriter.CreateWaveFile(newAudioFile, new WdlResamplingSampleProvider(audio.ToSampleProvider(), SoundItem.MAX_SAMPLERATE).ToWaveProvider16());
                log.Log($"Encoding audio to ADX...");
                AdxUtil.EncodeWav(newAudioFile, Path.Combine(baseDirectory, BgmFile), loopEnabled, loopStartSample, loopEndSample);
                audioFile = newAudioFile;
            }
            else
            {
                log.Log($"Encoding audio to ADX...");
                AdxUtil.EncodeAudio(audio, Path.Combine(baseDirectory, BgmFile), loopEnabled, loopStartSample, loopEndSample);
            }
            File.Copy(Path.Combine(baseDirectory, BgmFile), Path.Combine(iterativeDirectory, BgmFile), true);
            if (!string.Equals(audioFile, bgmCachedFile))
            {
                log.Log($"Attempting to cache audio file from {audioFile} to {bgmCachedFile}...");
                if (Path.GetExtension(audioFile).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(audioFile, bgmCachedFile, true);
                }
                else
                {
                    audio.Seek(0, SeekOrigin.Begin);
                    WaveFileWriter.CreateWaveFile(bgmCachedFile, audio.ToSampleProvider().ToWaveProvider16());
                }
            }
        }

        public IWaveProvider GetWaveProvider(ILogger log, bool loop)
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

            AdxDecoder decoder = new(adxBytes, log) { DoLoop = loop };
            return new AdxWaveProvider(decoder, decoder.Header.LoopInfo.EnabledInt == 1, decoder.LoopInfo.StartSample, decoder.LoopInfo.EndSample);
        }
    }
}
