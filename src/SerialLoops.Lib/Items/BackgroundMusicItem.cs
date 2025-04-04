﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using HaruhiChokuretsuLib.Audio.ADX;
using HaruhiChokuretsuLib.Util;
using NAudio.Flac;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib.Items;

public class BackgroundMusicItem : Item, ISoundItem
{
    private string _bgmFile;

    public string BgmFile { get; set; }
    public int Index { get; set; }
    public string BgmName { get; set; }
    public short? Flag { get; set; }
    public string CachedWaveFile { get; set; }

    public BackgroundMusicItem(string bgmFile, int index, Project project) : base(Path.GetFileNameWithoutExtension(bgmFile), ItemType.BGM)
    {
        _bgmFile = bgmFile;
        SetBgmFile(project);
        Index = index;
        BgmName = project.Extra.Bgms.FirstOrDefault(b => b.Index == Index)?.Name?.GetSubstitutedString(project) ?? "";
        Flag = project.Extra.Bgms.FirstOrDefault(b => b.Index == Index)?.Flag;
        DisplayName = string.IsNullOrEmpty(BgmName) ? Name : $"{Name} - {BgmName}";
        CanRename = string.IsNullOrEmpty(BgmName);
    }

    public void SetBgmFile(Project project, string oldProjDir = null)
    {
        if (!string.IsNullOrEmpty(oldProjDir))
        {
            _bgmFile = _bgmFile.Replace(oldProjDir, project.MainDirectory);
        }
        BgmFile = Path.GetRelativePath(project.IterativeDirectory, _bgmFile);
    }

    public override void Refresh(Project project, ILogger log)
    {
    }

    public void Replace(string audioFile, Project project, string bgmCachedFile, bool loopEnabled, uint loopStartSample, uint loopEndSample, ILogger log, IProgressTracker tracker, CancellationToken cancellationToken)
    {
        try
        {
            // The MP3 reader is able to create wave files but for whatever reason messes with the ADX encoder
            // So we just convert to WAV AOT
            if (Path.GetExtension(audioFile).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                string mp3ConvertedFile = Path.Combine(Path.GetDirectoryName(bgmCachedFile), $"{Path.GetFileNameWithoutExtension(bgmCachedFile)}-converted.wav");
                log.Log($"Converting {audioFile} to WAV...");
                tracker.Focus("Converting from MP3...", 1);
                using Mp3FileReaderBase mp3Reader = new(audioFile, new(wf => new Mp3FrameDecompressor(wf)));
                WaveFileWriter.CreateWaveFile(mp3ConvertedFile, mp3Reader.ToSampleProvider().ToWaveProvider16());
                audioFile = mp3ConvertedFile;
                tracker.Finished++;
            }
            // Ditto the Vorbis/Opus decoders
            else if (Path.GetExtension(audioFile).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                string oggConvertedFile = Path.Combine(Path.GetDirectoryName(bgmCachedFile), $"{Path.GetFileNameWithoutExtension(bgmCachedFile)}-converted.wav");
                log.Log($"Converting {audioFile} to WAV...");
                try
                {
                    tracker.Focus("Converting from Vorbis...", 1);
                    using VorbisWaveReader vorbisReader = new(audioFile);
                    WaveFileWriter.CreateWaveFile(oggConvertedFile, vorbisReader.ToSampleProvider().ToWaveProvider16());
                    audioFile = oggConvertedFile;
                    tracker.Finished++;
                }
                catch (Exception vEx)
                {
                    log.LogWarning($"Provided ogg was not vorbis; trying opus... (Exception: {vEx.Message})");
                    try
                    {
                        tracker.Focus("Converting from Opus...", 1);
                        using OggOpusFileReader opusReader = new(audioFile, log);
                        WaveFileWriter.CreateWaveFile(oggConvertedFile, opusReader.ToSampleProvider().ToWaveProvider16());
                        audioFile = oggConvertedFile;
                        tracker.Finished++;
                    }
                    catch (Exception opEx)
                    {
                        log.LogException(project.Localize("OggDecodeFailedMessage"), opEx);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.LogException("Failed converting audio file to WAV.", ex);
            log.LogWarning(audioFile);
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            log.Log("BGM replacement task canceled.");
            return;
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
        if (audio.WaveFormat.SampleRate > SoundItem.MAX_SAMPLERATE)
        {
            tracker.Focus("Downsampling...", 1);
            string newAudioFile = string.Empty;
            try
            {
                log.Log($"Downsampling audio from {audio.WaveFormat.SampleRate} to NDS max sample rate {SoundItem.MAX_SAMPLERATE}...");
                newAudioFile = Path.Combine(Path.GetDirectoryName(bgmCachedFile), $"{Path.GetFileNameWithoutExtension(bgmCachedFile)}-downsampled.wav");
                WaveFileWriter.CreateWaveFile(newAudioFile, new WdlResamplingSampleProvider(audio.ToSampleProvider(), SoundItem.MAX_SAMPLERATE).ToWaveProvider16());
            }
            catch (Exception ex)
            {
                log.LogException("Failed downsampling audio file.", ex);
                return;
            }
            tracker.Finished++;
            tracker.Focus("Encoding", 1);
            try
            {
                log.Log($"Encoding audio to ADX...");
                AdxUtil.EncodeWav(newAudioFile, Path.Combine(project.BaseDirectory, BgmFile), loopEnabled, loopStartSample, loopEndSample, cancellationToken);
                audioFile = newAudioFile;
            }
            catch (Exception ex)
            {
                log.LogException("Failed encoding audio file to ADX.", ex);
                return;
            }
            tracker.Finished++;
        }
        else
        {
            tracker.Focus("Encoding", 1);
            try
            {
                log.Log($"Encoding audio to ADX...");
                AdxUtil.EncodeAudio(audio, Path.Combine(project.BaseDirectory, BgmFile), loopEnabled, loopStartSample, loopEndSample, cancellationToken);
            }
            catch (Exception ex)
            {
                log.LogException("Failed encoding audio file to ADX.", ex);
                return;
            }
            tracker.Finished++;
        }
        tracker.Focus("Caching", 2);
        File.Copy(Path.Combine(project.BaseDirectory, BgmFile), Path.Combine(project.IterativeDirectory, BgmFile), true);
        tracker.Finished++;
        if (!string.Equals(audioFile, bgmCachedFile))
        {
            try
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
            catch (Exception ex)
            {
                log.LogException("Failed attempting to cache audio file", ex);
            }
        }
        tracker.Finished++;
    }

    public IWaveProvider GetWaveProvider(ILogger log, bool loop)
    {
        byte[] adxBytes = [];
        try
        {
            adxBytes = File.ReadAllBytes(_bgmFile);
        }
        catch
        {
            if (!File.Exists(_bgmFile))
            {
                log.LogError("Failed to load BGM file: file not found.");
                log.LogWarning(BgmFile);
            }
            else
            {
                log.LogError("Failed to load BGM file: file invalid.");
                log.LogWarning(BgmFile);
            }
        }
        try
        {
            AdxDecoder decoder = new(adxBytes, log);
            decoder.DoLoop = decoder.DoLoop && loop; // We manually handle looping here so that we can disable it when trying to export WAV files
            return new AdxWaveProvider(decoder, decoder.Header.LoopInfo.EnabledInt == 1, decoder.LoopInfo.StartSample, decoder.LoopInfo.EndSample);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to read BGM file; falling back to original...", ex);
            log.LogWarning(BgmFile);

            try
            {
                File.Copy(_bgmFile.Replace(Path.Combine("rom", "data"), Path.Combine("original")), _bgmFile, true);
                adxBytes = File.ReadAllBytes(_bgmFile);
            }
            catch (Exception nestedException)
            {
                log.LogException("Failed restoring BGM file.", nestedException);
                return null;
            }
            try
            {
                AdxDecoder decoder = new(adxBytes, log);
                decoder.DoLoop = decoder.DoLoop && loop;
                return new AdxWaveProvider(decoder, decoder.Header.LoopInfo.EnabledInt == 1, decoder.LoopInfo.StartSample, decoder.LoopInfo.EndSample);
            }
            catch (Exception nestedException)
            {
                log.LogException("Failed to decode original file too, giving up!", nestedException);
                return null;
            }
        }
    }

    public override string ToString() => DisplayName;
}
