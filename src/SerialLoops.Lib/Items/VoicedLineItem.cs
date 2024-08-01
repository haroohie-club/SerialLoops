using System;
using System.IO;
using HaruhiChokuretsuLib.Audio.ADX;
using HaruhiChokuretsuLib.Util;
using NAudio.Flac;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;

namespace SerialLoops.Lib.Items
{
    public class VoicedLineItem : Item, ISoundItem
    {
        private readonly string _vceFile;

        public string VoiceFile { get; set; }
        public int Index { get; set; }
        public AdxEncoding AdxType { get; set; }

        public VoicedLineItem(string voiceFile, int index, Project project) : base(Path.GetFileNameWithoutExtension(voiceFile), ItemType.Voice)
        {
            VoiceFile = Path.GetRelativePath(project.IterativeDirectory, voiceFile);
            _vceFile = voiceFile;
            Index = index;
        }
        
        public IWaveProvider GetWaveProvider(ILogger log, bool loop = false)
        {
            byte[] adxBytes = [];
            try
            {
                adxBytes = File.ReadAllBytes(_vceFile);
            }
            catch
            {
                if (!File.Exists(_vceFile))
                {
                    log.LogError("Failed to load voice file: file not found.");
                    log.LogWarning(_vceFile);
                }
                else
                {
                    log.LogError("Failed to load voice file: file invalid.");
                    log.LogWarning(_vceFile);
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

        public void Replace(string audioFile, string baseDirectory, string iterativeDirectory, string vceCachedFile, ILogger log)
        {
            // The MP3 decoder is able to create wave files but for whatever reason messes with the ADX encoder
            // So we just convert to WAV AOT
            if (Path.GetExtension(audioFile).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                using Mp3FileReaderBase mp3Reader = new(audioFile, new Mp3FileReaderBase.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf)));
                WaveFileWriter.CreateWaveFile(vceCachedFile, mp3Reader.ToSampleProvider().ToWaveProvider16());
                audioFile = vceCachedFile;
            }
            // Ditto the Vorbis decoder
            else if (Path.GetExtension(audioFile).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                using VorbisWaveReader vorbisReader = new(audioFile);
                WaveFileWriter.CreateWaveFile(vceCachedFile, vorbisReader.ToSampleProvider().ToWaveProvider16());
                audioFile = vceCachedFile;
            }
            using WaveStream audio = Path.GetExtension(audioFile).ToLower() switch
            {
                ".wav" => new WaveFileReader(audioFile),
                ".flac" => new FlacReader(audioFile),
                _ => null,
            };
            if (audio is null)
            {
                log.LogError("Invalid audio file selected.");
                log.LogWarning(audioFile);
                return;
            }
            if (audio.WaveFormat.Channels > 1 || audio.WaveFormat.SampleRate > SoundItem.MAX_SAMPLERATE)
            {
                string newAudioFile = "";
                if (audio.WaveFormat.Channels > 1)
                {
                    log.Log("Downmixing audio from stereo to mono for AHX conversion...");
                    newAudioFile = Path.Combine(Path.GetDirectoryName(vceCachedFile), $"{Path.GetFileNameWithoutExtension(vceCachedFile)}-downmixed.wav");
                    WaveFileWriter.CreateWaveFile(newAudioFile, audio.ToSampleProvider().ToMono().ToWaveProvider16());
                }
                if (audio.WaveFormat.SampleRate > SoundItem.MAX_SAMPLERATE)
                {
                    log.Log($"Downsampling audio from {audio.WaveFormat.SampleRate} to NDS max sample rate {SoundItem.MAX_SAMPLERATE}...");
                    string prevAudioFile = $"{newAudioFile}";
                    newAudioFile = Path.Combine(Path.GetDirectoryName(vceCachedFile), $"{Path.GetFileNameWithoutExtension(vceCachedFile)}-downsampled.wav");
                    if (!string.IsNullOrEmpty(prevAudioFile))
                    {
                        using WaveFileReader newAudio = new(prevAudioFile);
                        WaveFileWriter.CreateWaveFile(newAudioFile, new WdlResamplingSampleProvider(newAudio.ToSampleProvider(), SoundItem.MAX_SAMPLERATE).ToWaveProvider16());
                    }
                    else
                    {
                        WaveFileWriter.CreateWaveFile(newAudioFile, new WdlResamplingSampleProvider(audio.ToSampleProvider(), SoundItem.MAX_SAMPLERATE).ToWaveProvider16());
                    }
                }

                log.Log($"Encoding audio to AHX...");
                audioFile = newAudioFile;
                AdxUtil.EncodeWav(newAudioFile, Path.Combine(baseDirectory, VoiceFile), true);
            }
            else
            {
                log.Log($"Encoding audio to AHX...");
                AdxUtil.EncodeAudio(audio, Path.Combine(baseDirectory, VoiceFile), true);
            }
            File.Copy(Path.Combine(baseDirectory, VoiceFile), Path.Combine(iterativeDirectory, VoiceFile), true);
        }

        public override void Refresh(Project project, ILogger log)
        {
        }        
    }
}
