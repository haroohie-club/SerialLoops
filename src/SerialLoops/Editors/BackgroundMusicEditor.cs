using Eto.Forms;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class BackgroundMusicEditor : Editor
    {
        private BackgroundMusicItem _bgm;
        public SoundPlayerPanel BgmPlayer { get; set; }

        public BackgroundMusicEditor(BackgroundMusicItem item, Project project, ILogger log) : base(item, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _bgm = (BackgroundMusicItem)Description;
            BgmPlayer = new(_bgm, _log);

            Button extractButton = new() { Text = "Extract" };
            extractButton.Click += (obj, args) =>
            {
                SaveFileDialog saveFileDialog = new() { Title = "Save BGM as WAV" };
                saveFileDialog.Filters.Add(new() { Name = "WAV File", Extensions = new string[] { ".wav" } });
                if (saveFileDialog.ShowAndReportIfFileSelected(this))
                {
                    WaveFileWriter.CreateWaveFile(saveFileDialog.FileName, _bgm.GetWaveProvider(_log));
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
                    _ = new ProgressDialog(() => AdxUtil.EncodeWav(openFileDialog.FileName, _bgm.BgmFile), () =>
                    {
                        Content = GetEditorPanel();
                    }, tracker, "Replace BGM track");
                }
            };

            return new TableLayout(
                new TableRow(ControlGenerator.GetPlayerStackLayout(BgmPlayer, _bgm.BgmName, _bgm.Name)),
                new TableRow(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
                    Items =
                    {
                        replaceButton,
                        extractButton,
                    }
                }),
                new TableRow()
                );
        }
    }
}
