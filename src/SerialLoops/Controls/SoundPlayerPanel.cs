using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SerialLoops.Controls
{
    public class SoundPlayerPanel : Panel
    {
        private ILogger _log;
        private IWaveProvider _sound;
        private WaveOut _output { get; set; }
        private VolumeSampleProvider _sampleProvider { get; set; }

        public SoundPlayerPanel(IWaveProvider sound, ILogger log)
        {
            _log = log;
            _sound = sound;

            _output = new();
            _sampleProvider = new(_sound.ToSampleProvider());
            _output.DeviceNumber = -1;
            _output.Init(_sampleProvider);
            InitializeComponent();
        }

        public void InitializeComponent()
        {
            Button playPauseButton = new() { Text = "▶️", Font = new(Eto.Drawing.SystemFont.Default, 30.0f) };
            Slider volumeSlider = new() { Orientation = Orientation.Horizontal, MinValue = 0, MaxValue = 100, Value = 100 };
            playPauseButton.Click += PlayPauseButton_Click;
            volumeSlider.ValueChanged += VolumeSlider_ValueChanged;

            Content = new TableLayout(new TableRow(playPauseButton), new TableRow(volumeSlider));
        }

        private void PlayPauseButton_Click(object sender, System.EventArgs e)
        {
            Button playPauseButton = (Button)sender;
            switch (_output.PlaybackState)
            {
                case PlaybackState.Paused:
                case PlaybackState.Stopped:
                    _output.Play();
                    playPauseButton.Text = "⏸️";
                    break;

                case PlaybackState.Playing:
                    _output.Pause();
                    playPauseButton.Text = "▶️";
                    break;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, System.EventArgs e)
        {
            _sampleProvider.Volume = ((Slider)sender).Value / 100.0f;
        }
    }
}
