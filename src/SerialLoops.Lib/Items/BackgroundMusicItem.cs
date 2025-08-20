using System;
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
    private Project _project;

    public string BgmFile { get; set; }
    public int Index { get; set; }
    public string BgmName { get; set; }
    public short? Flag { get; set; }
    public string CachedWaveFile { get; set; }

    public BackgroundMusicItem(string bgmFile, int index, Project project) : base(Path.GetFileNameWithoutExtension(bgmFile), ItemType.BGM)
    {
        _bgmFile = bgmFile;
        _project = project;
        SetBgmFile(project);
        Index = index;
        BgmName = _project.Extra.Bgms.FirstOrDefault(b => b.Index == Index)?.Name?.GetSubstitutedString(_project) ?? "";
        Flag = _project.Extra.Bgms.FirstOrDefault(b => b.Index == Index)?.Flag;
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
                string mp3ConvertedFile = Path.Combine(Path.GetDirectoryName(bgmCachedFile)!, $"{Path.GetFileNameWithoutExtension(bgmCachedFile)}-converted.wav");
                log.Log($"Converting {audioFile} to WAV...");
                tracker.Focus(project.Localize("SoundEditorConvertingFromMp3Message"), 1);
                using Mp3FileReaderBase mp3Reader = new(audioFile, new(wf => new Mp3FrameDecompressor(wf)));
                WaveFileWriter.CreateWaveFile(mp3ConvertedFile, mp3Reader.ToSampleProvider().ToWaveProvider16());
                audioFile = mp3ConvertedFile;
                tracker.Finished++;
            }
            // Ditto the Vorbis/Opus decoders
            else if (Path.GetExtension(audioFile).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                string oggConvertedFile = Path.Combine(Path.GetDirectoryName(bgmCachedFile)!, $"{Path.GetFileNameWithoutExtension(bgmCachedFile)}-converted.wav");
                log.Log($"Converting {audioFile} to WAV...");
                try
                {
                    tracker.Focus(project.Localize("SoundEditorConvertingFromVorbisMessage"), 1);
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
                        tracker.Focus(project.Localize("SoundEditorConvertingFromOpusMessage"), 1);
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
            log.LogException(project.Localize("ErrorFailedConvertingAudioToWav"), ex);
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
            log.LogError(project.Localize("ErrorInvalidAudioSelected"));
            log.LogWarning(audioFile);
            return;
        }
        if (audio.WaveFormat.SampleRate > SoundItem.MAX_SAMPLERATE)
        {
            tracker.Focus(project.Localize("BgmItemDownsamplingProgressMessage"), 1);
            string newAudioFile;
            try
            {
                log.Log($"Downsampling audio from {audio.WaveFormat.SampleRate} to NDS max sample rate {SoundItem.MAX_SAMPLERATE}...");
                newAudioFile = Path.Combine(Path.GetDirectoryName(bgmCachedFile)!, $"{Path.GetFileNameWithoutExtension(bgmCachedFile)}-downsampled.wav");
                WaveFileWriter.CreateWaveFile(newAudioFile, new WdlResamplingSampleProvider(audio.ToSampleProvider(), SoundItem.MAX_SAMPLERATE).ToWaveProvider16());
            }
            catch (Exception ex)
            {
                log.LogException(project.Localize("ErrorFailedDownsamplingAudio"), ex);
                return;
            }
            tracker.Finished++;
            tracker.Focus(project.Localize("BgmItemEncodingProgressMessage"), 1);
            try
            {
                log.Log($"Encoding audio to ADX...");
                AdxUtil.EncodeWav(newAudioFile, Path.Combine(project.BaseDirectory, BgmFile), loopEnabled, loopStartSample, loopEndSample, cancellationToken);
                audioFile = newAudioFile;
            }
            catch (Exception ex)
            {
                log.LogException(project.Localize("ErrorFailedEncodingAudioAdx"), ex);
                return;
            }
            tracker.Finished++;
        }
        else
        {
            tracker.Focus(project.Localize("BgmItemEncodingProgressMessage"), 1);
            try
            {
                log.Log($"Encoding audio to ADX...");
                AdxUtil.EncodeAudio(audio, Path.Combine(project.BaseDirectory, BgmFile), loopEnabled, loopStartSample, loopEndSample, cancellationToken);
            }
            catch (Exception ex)
            {
                log.LogException(project.Localize("ErrorFailedEncodingAudioAdx"), ex);
                return;
            }
            tracker.Finished++;
        }
        tracker.Focus("BgmItemCachingMessage", 2);
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
                log.LogException(project.Localize("ErrorFailedToCacheAudioFile"), ex);
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
                log.LogError("ErrorBgmFileNotFound");
                log.LogWarning(BgmFile);
            }
            else
            {
                log.LogError(_project.Localize("ErrorInvalidBgmFile"));
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
            log.LogException(_project.Localize("ErrorFailedReadingBgmFile"), ex);
            log.LogWarning(BgmFile);

            try
            {
                File.Copy(_bgmFile.Replace(Path.Combine("rom", "data"), Path.Combine("original")), _bgmFile, true);
                adxBytes = File.ReadAllBytes(_bgmFile);
            }
            catch (Exception nestedException)
            {
                log.LogException(_project.Localize("ErrorFailedRestoringBgmFile"), nestedException);
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
                log.LogException(_project.Localize("ErrorFailedToDecodeOriginalAdx"), nestedException);
                return null;
            }
        }
    }

    public override string ToString() => DisplayName;
}
