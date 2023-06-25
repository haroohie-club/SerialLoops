using Eto.Forms;
using HaruhiChokuretsuLib.Audio.ADX;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using System.IO;
using System.Linq;

namespace SerialLoops.Editors
{
    public class BackgroundMusicEditor : Editor
    {
        private BackgroundMusicItem _bgm;
        private bool _loopEnabled;
        private uint _loopStartSample;
        private uint _loopEndSample;

        public SoundPlayerPanel BgmPlayer { get; set; }

        private string _bgmCachedFile;

        public BackgroundMusicEditor(BackgroundMusicItem item, Project project, ILogger log) : base(item, log, project)
        {
            _bgmCachedFile = Path.Combine(project.Config.CachesDirectory, "bgm", $"{_bgm.Name}.wav");
        }

        public override Container GetEditorPanel()
        {
            _bgm = (BackgroundMusicItem)Description;
            BgmPlayer = new(_bgm, _log);

            TextBox bgmTitleBox = null;
            if ((_bgm.BgmName?.Length ?? 0) > 0)
            {
                bgmTitleBox = new() { Text = _bgm.BgmName, Width = 200 };
                bgmTitleBox.TextChanged += (obj, args) =>
                {
                    _project.Extra.Bgms[_project.Extra.Bgms.IndexOf(_project.Extra.Bgms.First(b => b.Name.GetSubstitutedString(_project) == _bgm.BgmName))].Name = bgmTitleBox.Text.GetOriginalString(_project);
                    _bgm.BgmName = bgmTitleBox.Text;
                    UpdateTabTitle(false, bgmTitleBox);
                };
            }

            Button loopSettingsButton = new() { Text = "Manage Loop" };
            loopSettingsButton.Click += (obj, args) =>
            {
                BgmPlayer.Stop();
                LoopyProgressTracker tracker = new("Adjusting Loop Info");
                if (!File.Exists(_bgmCachedFile))
                {
                    _ = new ProgressDialog(() => WaveFileWriter.CreateWaveFile(_bgmCachedFile, _bgm.GetWaveProvider(_log, false)), () => { }, tracker, "Caching BGM");
                }
                string loopAdjustedWav = Path.Combine(Path.GetDirectoryName(_bgmCachedFile), $"{Path.GetFileNameWithoutExtension(_bgmCachedFile)}-loop.wav");
                File.Copy(_bgmCachedFile, loopAdjustedWav, true);
                using WaveFileReader reader = new(loopAdjustedWav);
                BgmLoopPropertiesDialog loopDialog = new(reader, _bgm.Name, _log, ((AdxWaveProvider)BgmPlayer.Sound).LoopEnabled, ((AdxWaveProvider)BgmPlayer.Sound).LoopStartSample, ((AdxWaveProvider)BgmPlayer.Sound).LoopEndSample);
                loopDialog.Closed += (obj, args) =>
                {
                    if (loopDialog.SaveChanges)
                    {
                        _loopEnabled = loopDialog.LoopPreview.LoopEnabled;
                        _loopStartSample = loopDialog.LoopPreview.StartSample;
                        _loopEndSample = loopDialog.LoopPreview.EndSample;
                        LoopyProgressTracker tracker = new();
                        BgmPlayer.Stop();
                        Shared.AudioReplacementCancellation.Cancel();
                        Shared.AudioReplacementCancellation = new();
                        _ = new ProgressDialog(() =>
                        {
                            _bgm.Replace(_bgmCachedFile, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, tracker, Shared.AudioReplacementCancellation.Token);
                            reader.Dispose();
                            File.Delete(loopAdjustedWav);
                        },
                        () =>
                        {
                            Content = GetEditorPanel();
                        }, tracker, "Set BGM loop info");
                    }
                };
                loopDialog.ShowModal(this);
            };
            Button volumeSettingsButton = new() { Text = "Adjust Volume" };
            volumeSettingsButton.Click += (obj, args) =>
            {
                BgmPlayer.Stop();
                LoopyProgressTracker tracker = new("Adjusting Volume");
                if (!File.Exists(_bgmCachedFile))
                {
                    _ = new ProgressDialog(() => WaveFileWriter.CreateWaveFile(_bgmCachedFile, _bgm.GetWaveProvider(_log, false)), () => { }, tracker, "Caching BGM");
                }
                string volumeAdjustedWav = Path.Combine(Path.GetDirectoryName(_bgmCachedFile), $"{Path.GetFileNameWithoutExtension(_bgmCachedFile)}-volume.wav");
                File.Copy(_bgmCachedFile, volumeAdjustedWav, true);
                using WaveFileReader reader = new(volumeAdjustedWav);
                BgmVolumePropertiesDialog volumeDialog = new(reader, _bgm.Name, _log);
                volumeDialog.Closed += (obj, args) =>
                {
                    if (volumeDialog.SaveChanges)
                    {
                        LoopyProgressTracker tracker = new() { Total = 2 };
                        BgmPlayer.Stop();
                        Shared.AudioReplacementCancellation.Cancel();
                        Shared.AudioReplacementCancellation = new();
                        _ = new ProgressDialog(() =>
                        {
                            WaveFileWriter.CreateWaveFile(_bgmCachedFile, volumeDialog.VolumePreview.GetWaveProvider(_log, false));
                            tracker.Finished++;
                            _bgm.Replace(_bgmCachedFile, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, tracker, Shared.AudioReplacementCancellation.Token);
                            tracker.Finished++;
                            reader.Dispose();
                            File.Delete(volumeAdjustedWav);
                        },
                        () =>
                        {
                            Content = GetEditorPanel();
                        }, tracker, "Set BGM volume");
                    }
                };
                volumeDialog.ShowModal(this);
            };

            Button extractButton = new() { Text = "Extract" };
            extractButton.Click += (obj, args) =>
            {
                SaveFileDialog saveFileDialog = new() { Title = "Save BGM as WAV" };
                saveFileDialog.Filters.Add(new() { Name = "WAV File", Extensions = new string[] { ".wav" } });
                if (saveFileDialog.ShowAndReportIfFileSelected(this))
                {
                    LoopyProgressTracker tracker = new();
                    _ = new ProgressDialog(() => WaveFileWriter.CreateWaveFile(saveFileDialog.FileName, _bgm.GetWaveProvider(_log, false)),
                        () => { }, tracker, "Exporting BGM");
                }
            };

            Button replaceButton = new() { Text = "Replace" };
            replaceButton.Click += (obj, args) =>
            {
                OpenFileDialog openFileDialog = new() { Title = "Replace BGM" };
                openFileDialog.Filters.Add(new() { Name = "Supported Audio Files", Extensions = new string[] { ".wav", ".flac", ".mp3", ".ogg" } });
                openFileDialog.Filters.Add(new() { Name = "WAV files", Extensions = new string[] { ".wav" } });
                openFileDialog.Filters.Add(new() { Name = "FLAC files", Extensions = new string[] { ".flac" } });
                openFileDialog.Filters.Add(new() { Name = "MP3 files", Extensions = new string[] { ".mp3" } });
                openFileDialog.Filters.Add(new() { Name = "Vorbis files", Extensions = new string[] { ".ogg" } });
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    LoopyProgressTracker tracker = new("Replacing BGM");
                    BgmPlayer.Stop();
                    Shared.AudioReplacementCancellation.Cancel();
                    Shared.AudioReplacementCancellation = new();
                    _ = new ProgressDialog(() => _bgm.Replace(openFileDialog.FileName, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, tracker, Shared.AudioReplacementCancellation.Token), () =>
                    {
                        Content = GetEditorPanel();
                    }, tracker, "Replace BGM track");
                }
            };

            Button restoreButton = new() { Text = "Restore" };
            restoreButton.Click += (obj, args) =>
            {
                BgmPlayer.Stop();
                File.Delete(_bgmCachedFile); // Clear the cached WAV as we're restoring the original ADX
                File.Copy(Path.Combine(_project.BaseDirectory, "original", "bgm", Path.GetFileName(_bgm.BgmFile)), Path.Combine(_project.BaseDirectory, _bgm.BgmFile), true);
                File.Copy(Path.Combine(_project.IterativeDirectory, "original", "bgm", Path.GetFileName(_bgm.BgmFile)), Path.Combine(_project.IterativeDirectory, _bgm.BgmFile), true);
                Content = GetEditorPanel();
            };

            return new TableLayout(
                new TableRow(ControlGenerator.GetPlayerStackLayout(BgmPlayer, bgmTitleBox, _bgm.Name, _bgm.Flag)),
                new TableRow(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
                    Padding = 2,
                    Items =
                    {
                        loopSettingsButton,
                        volumeSettingsButton,
                    }
                }),
                new TableRow(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
                    Padding = 2,
                    Items =
                    {
                        replaceButton,
                        extractButton,
                        restoreButton,
                    }
                }),
                new TableRow()
                );
        }
    }
}
