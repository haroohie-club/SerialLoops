﻿using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;

namespace SerialLoops.Controls
{
    public class SoundPlayerPanel : Panel
    {
        private ILogger _log;
        private IWaveProvider _sound;
        private SoundPlayer _player;
        private Button _playPauseButton;

        public SoundPlayerPanel(IWaveProvider sound, ILogger log)
        {
            _log = log;
            _sound = sound;
            _log.Log("Attempting to initialize sound player...");
            _player = new SoundPlayer();
            _player.Initialize(_sound);
            _log.Log("Sound player successfully initialized.");

            InitializeComponent();
        }

        public void InitializeComponent()
        {
            _playPauseButton = new() { Text = "▶️", Font = new(Eto.Drawing.SystemFont.Default, 30.0f) };
            _playPauseButton.Click += PlayPauseButton_Click;

            Content = new TableLayout(new TableRow(_playPauseButton), new TableRow());
        }

        public void Stop()
        {
            _player.Stop();
        }

        private void PlayPauseButton_Click(object sender, System.EventArgs e)
        {
            if (_player.PlaybackState == PlaybackState.Playing)
            {
                _player.Pause();
                _playPauseButton.Text = "▶️";
            }
            else
            {
                _player.Play();
                _playPauseButton.Text = "⏸️";
            }
        }
    }
}
