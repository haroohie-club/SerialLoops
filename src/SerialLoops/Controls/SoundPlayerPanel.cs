using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Controls
{
    public class SoundPlayerPanel : Panel
    {
        private ILogger _log;
        private IWaveProvider _sound;
        private ISoundItem _item;
        private SoundPlayer _player;
        private Button _playPauseButton;
        private Button _stopButton;

        public SoundPlayerPanel(ISoundItem item, ILogger log)
        {
            _log = log;
            _item = item;
            _log.Log("Attempting to initialize sound player...");
            _player = new SoundPlayer();
            _player.Initialize(_sound);
            _log.Log("Sound player successfully initialized.");

            InitializeComponent();
        }

        public void InitializeComponent()
        {
            _playPauseButton = new() { Image = ControlGenerator.GetIcon("Play", _log), Width = 25, Height = 25 };
            _stopButton = new() { Image = ControlGenerator.GetIcon("Stop", _log), Width = 25, Height = 25, Enabled = false };
            _sound = _item.GetWaveProvider(_log);
            _playPauseButton.Click += PlayPauseButton_Click;

            Content = new StackLayout
            {
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                Items =
                {
                    _playPauseButton,
                    _stopButton
                }
            };
        }

        public void Stop()
        {
            _player.Stop();
            _sound = _item.GetWaveProvider(_log);
            _stopButton.Enabled = false;
        }

        private void PlayPauseButton_Click(object sender, System.EventArgs e)
        {
            _stopButton.Enabled = true;
            if (_player.PlaybackState == PlaybackState.Playing)
            {
                _player.Pause();
                _playPauseButton.Image = ControlGenerator.GetIcon("Play", _log);
            }
            else
            {
                _player.Play();
                _playPauseButton.Image = ControlGenerator.GetIcon("Pause", _log);
            }
        }
    }
}
