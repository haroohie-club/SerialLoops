using System;
using System.IO;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio.ADX;
using HaruhiChokuretsuLib.Util;
using NAudio.Flac;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib.Items;

public class VoicedLineItem : Item, ISoundItem
{
    private string _vceFile;
    private Func<string, string> _localize;

    public string VoiceFile { get; set; }
    public int Index { get; set; }
    public AdxEncoding AdxType { get; set; }

    public VoicedLineItem(string voiceFile, int index, Project project) : base(Path.GetFileNameWithoutExtension(voiceFile), ItemType.Voice)
    {
        _vceFile = voiceFile;
        _localize = project.Localize;
        SetVoiceFile(project);
        Index = index;
    }

    public void SetVoiceFile(Project project, string oldProjDir = null)
    {
        if (!string.IsNullOrEmpty(oldProjDir))
        {
            _vceFile = _vceFile.Replace(oldProjDir, project.MainDirectory);
        }
        VoiceFile = Path.GetRelativePath(project.IterativeDirectory, _vceFile);
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
                log.LogError(_localize("ErrorVoiceFileNotFound"));
                log.LogWarning(_vceFile);
            }
            else
            {
                log.LogError(_localize("ErrorInvalidVoiceFile"));
                log.LogWarning(_vceFile);
            }
        }

        AdxType = (AdxEncoding)adxBytes[4];

        IAdxDecoder decoder;
        if (AdxType is AdxEncoding.Ahx10 or AdxEncoding.Ahx11)
        {
            decoder = new AhxDecoder(adxBytes, log);
        }
        else
        {
            decoder = new AdxDecoder(adxBytes, log);
        }

        return new AdxWaveProvider(decoder);
    }

    public void Replace(string audioFile, Project project, string vceCachedFile, ILogger log, VoiceMapFile.VoiceMapEntry vceMapEntry = null, bool asAhx = false)
    {
        // The MP3 decoder is able to create wave files but for whatever reason messes with the ADX encoder
        // So we just convert to WAV AOT
        if (Path.GetExtension(audioFile).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            using Mp3FileReaderBase mp3Reader = new(audioFile, new(wf => new Mp3FrameDecompressor(wf)));
            WaveFileWriter.CreateWaveFile(vceCachedFile, mp3Reader.ToSampleProvider().ToWaveProvider16());
            audioFile = vceCachedFile;
        }
        // Ditto the Vorbis/Opus decoders
        else if (Path.GetExtension(audioFile).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using VorbisWaveReader vorbisReader = new(audioFile);
                WaveFileWriter.CreateWaveFile(vceCachedFile, vorbisReader.ToSampleProvider().ToWaveProvider16());
                audioFile = vceCachedFile;
            }
            catch (Exception vEx)
            {
                log.LogWarning($"Provided ogg was not vorbis; trying opus... (Exception: {vEx.Message})");
                try
                {
                    using OggOpusFileReader opusReader = new(audioFile, log);
                    WaveFileWriter.CreateWaveFile(vceCachedFile, opusReader.ToSampleProvider().ToWaveProvider16());
                    audioFile = vceCachedFile;
                }
                catch (Exception opEx)
                {
                    log.LogException(project.Localize("OggDecodeFailedMessage"), opEx);
                }
            }
        }

        using WaveStream audio = Path.GetExtension(audioFile).ToLower() switch
        {
            ".wav" => new WaveFileReader(audioFile),
            ".flac" => new FlacReader(audioFile),
            _ => null,
        };

        if (audio is null)
        {
            log.LogError(project.Localize("ErrorInvalidAudioSelected"));
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

            if (asAhx)
            {
                log.Log("Encoding audio to AHX...");
                AdxUtil.EncodeWav(newAudioFile, Path.Combine(project.BaseDirectory, VoiceFile), true);
            }
            else
            {
                log.Log("Encoding audio to ADX...");
                AdxUtil.EncodeWav(newAudioFile, Path.Combine(project.BaseDirectory, VoiceFile), false);
            }
        }
        else
        {
            if (asAhx)
            {
                log.Log("Encoding audio to AHX...");
                AdxUtil.EncodeAudio(audio, Path.Combine(project.BaseDirectory, VoiceFile), true);
            }
            else
            {
                log.Log("Encoding audio to ADX...");
                AdxUtil.EncodeAudio(audio, Path.Combine(project.BaseDirectory, VoiceFile), false);
            }
        }
        File.Copy(Path.Combine(project.BaseDirectory, VoiceFile), Path.Combine(project.IterativeDirectory, VoiceFile), true);

        // Adjust subtitle length to new voice item length
        if (vceMapEntry is not null)
        {
            vceMapEntry.Timer = (int)(audio.TotalTime.TotalSeconds * 180 + 30);
            UnsavedChanges = true;
        }
    }

    public override void Refresh(Project project, ILogger log)
    {
    }

    public override string ToString() => DisplayName ?? "NONE";
}
