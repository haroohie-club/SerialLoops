using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util.WaveformRenderer;
using SerialLoops.Utility;
using System;
using System.IO;

namespace SerialLoops.Dialogs
{
    public class BgmLoopPropertiesDialog : Dialog
    {
        private ILogger _log;

        public bool SaveChanges { get; set; } = false;
        public BgmLoopPreviewItem LoopPreview { get; set; }
        public SKGuiImage Waveform { get; set; }
        public SoundPlayerPanel LoopPreviewPlayer { get; set; }

        public BgmLoopPropertiesDialog(WaveStream wav, string title, ILogger log, bool loopEnabled, uint startSample = 0, uint endSample = 0)
        {
            _log = log;
            Title = string.Format(Application.Instance.Localize(this, "{0} - Manage Loop"), title);
            Waveform = new(WaveformRenderer.Render(wav, WaveFormRendererSettings.StandardSettings));
            if (endSample == 0)
            {
                endSample = (uint)(wav.Length / (wav.WaveFormat.BitsPerSample / 8));
            }
            LoopPreview = new(wav, loopEnabled, startSample, endSample);
            LoopPreviewPlayer = new(LoopPreview, _log);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            CheckBox loopEnabledCheckBox = new()
            {
                Text = Application.Instance.Localize(this, "Loop?"),
                Checked = LoopPreview.LoopEnabled,
            };
            loopEnabledCheckBox.CheckedChanged += (obj, args) =>
            {
                LoopPreview.LoopEnabled = loopEnabledCheckBox.Checked ?? true;
            };

            Slider startSampleSlider = new()
            {
                MinValue = 0,
                MaxValue = (int)(LoopPreview.Wave.Length / LoopPreview.Wave.WaveFormat.BitsPerSample * 8 / LoopPreview.Wave.WaveFormat.Channels),
                Value = (int)LoopPreview.StartSample,
                Width = Waveform.Width
            };
            Slider endSampleSlider = new()
            {
                MinValue = 0,
                MaxValue = (int)(LoopPreview.Wave.Length / LoopPreview.Wave.WaveFormat.BitsPerSample * 8 / LoopPreview.Wave.WaveFormat.Channels),
                Value = (int)LoopPreview.EndSample,
                Width = Waveform.Width
            };

            NumericStepper startSampleBox = new()
            {
                MinValue = 0.0,
                MaxValue = LoopPreview.GetTimestampFromSample((uint)(LoopPreview.Wave.Length / LoopPreview.Wave.WaveFormat.BitsPerSample * 8 / LoopPreview.Wave.WaveFormat.Channels)),
                MaximumDecimalPlaces = 4,
                Value = LoopPreview.GetTimestampFromSample(LoopPreview.StartSample),
            };
            NumericStepper endSampleBox = new()
            {
                MinValue = startSampleBox.Value,
                MaxValue = LoopPreview.GetTimestampFromSample((uint)(LoopPreview.Wave.Length / LoopPreview.Wave.WaveFormat.BitsPerSample * 8 / LoopPreview.Wave.WaveFormat.Channels)),
                MaximumDecimalPlaces = 4,
                Value = LoopPreview.GetTimestampFromSample(LoopPreview.EndSample),
            };
            startSampleBox.MaxValue = endSampleBox.Value;

            startSampleSlider.ValueChanged += (obj, args) =>
            {
                if (startSampleSlider.Value > endSampleSlider.Value)
                {
                    startSampleSlider.Value = endSampleSlider.Value;
                    return;
                }
                LoopPreview.StartSample = (uint)startSampleSlider.Value;
                startSampleBox.Value = LoopPreview.GetTimestampFromSample(LoopPreview.StartSample);
                endSampleBox.MinValue = startSampleBox.Value;
                LoopPreviewPlayer.Stop();
            };
            endSampleSlider.ValueChanged += (obj, args) =>
            {
                if (endSampleSlider.Value < startSampleSlider.Value)
                {
                    endSampleSlider.Value = startSampleSlider.Value;
                    return;
                }
                LoopPreview.EndSample = (uint)endSampleSlider.Value;
                endSampleBox.Value = LoopPreview.GetTimestampFromSample(LoopPreview.EndSample);
                startSampleBox.MaxValue = endSampleBox.Value;
                LoopPreviewPlayer.Stop();
            };
            startSampleBox.ValueChanged += (obj, args) =>
            {
                LoopPreview.StartSample = LoopPreview.GetSampleFromTimestamp(startSampleBox.Value);
                startSampleSlider.Value = (int)LoopPreview.StartSample;
                endSampleBox.MinValue = startSampleBox.Value;
                LoopPreviewPlayer.Stop();
            };
            endSampleBox.ValueChanged += (obj, args) =>
            {
                LoopPreview.EndSample = LoopPreview.GetSampleFromTimestamp(endSampleBox.Value);
                endSampleSlider.Value = (int)LoopPreview.EndSample;
                startSampleBox.MaxValue = endSampleBox.Value;
                LoopPreviewPlayer.Stop();
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
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Padding = 10,
                Items =
                {
                    new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 10,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Items =
                        {
                            LoopPreviewPlayer,
                            loopEnabledCheckBox,
                            new StackLayout { Height = Waveform.Height - 65 },
                            ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Start"), startSampleBox),
                            ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "End"), endSampleBox),
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 5,
                        Items =
                        {
                            Waveform,
                            startSampleSlider,
                            endSampleSlider,
                            new StackLayout
                            {
                                Orientation = Orientation.Vertical,
                                Spacing = 3,
                                HorizontalContentAlignment = HorizontalAlignment.Right,
                                Width = Waveform.Width,
                                Items =
                                {
                                    new StackLayout // Nesting this StackLayout shoves the buttons to the right appropriately
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Spacing = 3,
                                        Items =
                                        {
                                            saveButton,
                                            cancelButton,
                                        }
                                    }
                                },
                            }
                        }
                    },
                }
            };
        }
    }

    public class BgmLoopPreviewItem : ISoundItem
    {
        public WaveStream Wave { get; set; }
        public bool LoopEnabled { get; set; }
        public uint StartSample { get; set; }
        public uint EndSample { get; set; }

        public BgmLoopPreviewItem(WaveStream wav, bool loopEnabled, uint startSample, uint endSample)
        {
            Wave = wav;
            LoopEnabled = loopEnabled;
            StartSample = startSample;
            EndSample = endSample;
        }

        public IWaveProvider GetWaveProvider(ILogger log, bool loop)
        {
            MemoryStream stream = new();
            RawSourceWaveStream loopStream = new(stream, Wave.WaveFormat);
            byte[] fiveSecondBuffer = new byte[5 * (Wave.WaveFormat.BitsPerSample / 4) * Wave.WaveFormat.SampleRate];

            long startLoc = StartSample * Wave.WaveFormat.BitsPerSample / 8 * Wave.WaveFormat.Channels;
            long endLoc = EndSample * (Wave.WaveFormat.BitsPerSample / 8 * Wave.WaveFormat.Channels);

            Wave.Seek(Math.Max(startLoc, endLoc - fiveSecondBuffer.Length), SeekOrigin.Begin);
            Wave.Read(fiveSecondBuffer, 0, fiveSecondBuffer.Length);
            stream.Write(fiveSecondBuffer);

            Wave.Seek(startLoc, SeekOrigin.Begin);
            Wave.Read(fiveSecondBuffer, 0, (int)Math.Min(Wave.Length - startLoc, fiveSecondBuffer.Length));
            stream.Write(fiveSecondBuffer);

            loopStream.Seek(0, SeekOrigin.Begin);
            return loopStream.ToSampleProvider().ToWaveProvider16();
        }

        public double GetTimestampFromSample(uint sample)
        {
            return (double)sample / Wave.WaveFormat.SampleRate;
        }
        public uint GetSampleFromTimestamp(double timestamp)
        {
            return (uint)(timestamp * Wave.WaveFormat.SampleRate);
        }
    }
}
