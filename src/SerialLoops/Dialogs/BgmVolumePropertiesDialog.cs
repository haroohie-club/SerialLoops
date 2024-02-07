using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util.WaveformRenderer;
using SerialLoops.Utility;
using System.IO;

namespace SerialLoops.Dialogs
{
    public class BgmVolumePropertiesDialog : Dialog
    {
        private ILogger _log;
        private WaveStream _wav;
        private StackLayout _waveformLayout;
        private long _waveLength;

        public bool SaveChanges { get; set; } = false;
        public BgmVolumePreviewItem VolumePreview { get; set; }
        public SKGuiImage Waveform { get; set; }
        public SoundPlayerPanel VolumePreviewPlayer { get; set; }

        public BgmVolumePropertiesDialog(WaveStream wav, string title, ILogger log)
        {
            _log = log;
            Title = string.Format(Application.Instance.Localize(this, "{0} - Adjust Volume"), title);
            _wav = wav;
            Waveform = new(WaveformRenderer.Render(wav, WaveFormRendererSettings.StandardSettings));
            _waveformLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Items =
                {
                    Waveform,
                }
            };
            _waveLength = wav.Length;
            VolumePreview = new(wav);
            VolumePreviewPlayer = new(VolumePreview, _log);

            InitializeComponent();
        }

        public void InitializeComponent()
        {
            Slider volumeSlider = new()
            {
                MinValue = 0,
                MaxValue = 200,
                Height = Waveform.Height,
                Value = 100,
                Orientation = Orientation.Vertical,
            };
            volumeSlider.ValueChanged += (obj, args) =>
            {
                _wav.Seek(0, SeekOrigin.Begin);
                VolumePreview.SetVolume(volumeSlider.Value);
                _wav.Seek(0, SeekOrigin.Begin);
                Waveform = new(WaveformRenderer.Render(VolumePreview.Provider, _waveLength, WaveFormRendererSettings.StandardSettings));
                _wav.Seek(0, SeekOrigin.Begin);
                _waveformLayout.Items.Clear();
                _waveformLayout.Items.Add(Waveform);
                VolumePreviewPlayer.Stop();
            };

            Button saveButton = new() { Text = Application.Instance.Localize(this, "Save") };
            Button cancelButton = new() { Text = Application.Instance.Localize(this, "Cancel") };
            saveButton.Click += (obj, args) =>
            {
                SaveChanges = true;
                Close();
            };
            cancelButton.Click += (obj, args) =>
            {
                Close();
            };

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Padding = 10,
                Items =
                {
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items =
                        {
                            VolumePreviewPlayer,
                            volumeSlider,
                            _waveformLayout,
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 3,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Width = Waveform.Width + 105,
                        Items =
                        {
                            new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 3,
                                Items =
                                {
                                    saveButton,
                                    cancelButton,
                                }
                            },
                        }
                    }
                }
            };
        }
    }

    public class BgmVolumePreviewItem : ISoundItem
    {
        private WaveStream _wav;
        public VolumeSampleProvider Provider { get; set; }

        public BgmVolumePreviewItem(WaveStream wav)
        {
            _wav = wav;
            _wav.Seek(0, SeekOrigin.Begin);
            Provider = new(wav.ToSampleProvider());
        }

        public void SetVolume(int volume)
        {
            Provider.Volume = volume / 100f;
        }

        public IWaveProvider GetWaveProvider(ILogger log, bool loop)
        {
            _wav.Seek(0, SeekOrigin.Begin);
            return Provider.ToWaveProvider16();
        }
    }
}
