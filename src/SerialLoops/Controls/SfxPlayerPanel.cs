using Eto.Forms;
using GotaSequenceLib.Playback;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;

namespace SerialLoops.Controls
{
    public class SfxPlayerPanel : Panel
    {
        private ILogger _log;
        private Player _player;
        private Button _playPauseButton;
        private Button _stopButton;

        public SfxPlayerPanel(Player player, ILogger log)
        {
            _log = log;
            _player = player;

            InitializeComponent();
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
                }
            };
        }

        public void Stop()
        {
            _stopButton.Enabled = false;
            _playPauseButton.Image = ControlGenerator.GetIcon("Play", _log);
            _player.Stop();
        }

        private void PlayPauseButton_Click(object sender, EventArgs e)
        {
            _stopButton.Enabled = true;
            if (_player.State == PlayerState.Playing)
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
