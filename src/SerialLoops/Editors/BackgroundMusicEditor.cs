using Eto.Forms;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System.IO;

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

            Button settingsButton = new() { Text = "Manage Looping" };
            settingsButton.Click += (obj, args) =>
            {
                LoopyProgressTracker tracker = new();
                if (!File.Exists(_bgmCachedFile))
                {
                    _ = new ProgressDialog(() => WaveFileWriter.CreateWaveFile(_bgmCachedFile, _bgm.GetWaveProvider(_log, false)), () => { }, tracker, "Caching BGM");
                }
                using WaveFileReader reader = new(_bgmCachedFile);
                BgmPropertiesDialog propertiesDialog = new(reader, _log, ((AdxWaveProvider)BgmPlayer.Sound).LoopEnabled, ((AdxWaveProvider)BgmPlayer.Sound).LoopStartSample, ((AdxWaveProvider)BgmPlayer.Sound).LoopEndSample);
                propertiesDialog.ShowModal(this);
                propertiesDialog.Closed += (obj, args) =>
                {
                    if (propertiesDialog.SaveChanges)
                    {
                        _loopEnabled = propertiesDialog.LoopPreview.LoopEnabled;
                        _loopStartSample = propertiesDialog.LoopPreview.StartSample;
                        _loopEndSample = propertiesDialog.LoopPreview.EndSample;
                        LoopyProgressTracker tracker = new();
                        BgmPlayer.Stop();
                        _ = new ProgressDialog(() => _bgm.Replace(_bgmCachedFile, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample), () =>
                        {
                            Content = GetEditorPanel();
                        }, tracker, "Replace BGM track");
                    }
                };
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
                openFileDialog.Filters.Add(new() { Name = "WAV File", Extensions = new string[] { ".wav" } });
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    LoopyProgressTracker tracker = new();
                    BgmPlayer.Stop();
                    _ = new ProgressDialog(() => _bgm.Replace(openFileDialog.FileName, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample), () =>
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
                new TableRow(ControlGenerator.GetPlayerStackLayout(BgmPlayer, _bgm.BgmName, _bgm.Name)),
                new TableRow(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
                    Items =
                    {
                        settingsButton,
                    }
                }),
                new TableRow(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
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
