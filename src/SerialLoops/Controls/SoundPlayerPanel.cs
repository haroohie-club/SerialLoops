using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;

namespace SerialLoops.Controls
{
    public class SoundPlayerPanel : Panel
    {
        private ILogger _log;
        private ISoundItem _item;
        private SoundPlayer _player;
        private Button _playPauseButton;
        private Button _stopButton;

        public IWaveProvider Sound { get; private set; }
        public SKGuiImage Waveform { get; private set; }

        public SoundPlayerPanel(ISoundItem item, ILogger log)
        {
            _log = log;
            _item = item;

            InitializePlayer();
            InitializeComponent();
        }

        private void InitializePlayer()
        {
            _log.Log("Attempting to initialize sound player...");
            _player = new();
            Sound = _item.GetWaveProvider(_log, true);
            _player.Initialize(Sound);
            _log.Log("Sound player successfully initialized.");
        }

        public void InitializeComponent()
        {
            _playPauseButton = new() { Image = ControlGenerator.GetIcon("Play", _log), Width = 25, Height = 25 };
            _stopButton = new() { Image = ControlGenerator.GetIcon("Stop", _log), Width = 25, Height = 25, Enabled = false };
            _playPauseButton.Click += PlayPauseButton_Click;
            _stopButton.Click += StopButton_Click;

            Content = new StackLayout
            {
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                Items =
                {
                    _playPauseButton,
                    _stopButton,
                    Waveform,
                }
            };
        }

        public void Stop()
        {
            _stopButton.Enabled = false;
            _playPauseButton.Image = ControlGenerator.GetIcon("Play", _log);
            _player.Stop();
            InitializePlayer();
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

        private void StopButton_Click(object sender, EventArgs e)
        {
            Stop();
        }
    }
}
