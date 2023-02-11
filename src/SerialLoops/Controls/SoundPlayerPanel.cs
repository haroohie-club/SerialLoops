﻿using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using LibVLCSharp.Shared;
using NAudio.Wave;
using SerialLoops.Lib.Util;
using System;
using System.IO;

namespace SerialLoops.Controls
{
    public class SoundPlayerPanel : Panel
    {
        private ILogger _log;
        private IWaveProvider _sound;
        private MediaPlayer _player;
        private Button _playPauseButton;

        public SoundPlayerPanel(IWaveProvider sound, ILogger log)
        {
            _log = log;
            _sound = sound;

            MemoryStream memoryStream = new();
            WaveProviderStream waveStream = new(_sound);
            WaveFileWriter writer = new(memoryStream, _sound.WaveFormat);
            waveStream.CopyTo(writer);
            memoryStream.Position = 0;
            LibVLC libVlc;
            try
            {
                libVlc = new();
            }
            catch (VLCException exc)
            {
                _log.LogError($"Error instantiating VLC -- if you're using Linux, ensure you've followed the instructions on installing libvlc for your platform.\nInner exception: {exc.Message}\n\n{exc.StackTrace}");
                return;
            }
            StreamMediaInput mediaInput = new(memoryStream);
            Media media = new(libVlc, mediaInput);
            _player = new(media);

            InitializeComponent();
        }

        public void InitializeComponent()
        {
            _playPauseButton = new() { Text = "▶️", Font = new(Eto.Drawing.SystemFont.Default, 30.0f) };
            Slider volumeSlider = new() { Orientation = Orientation.Horizontal, MinValue = 0, MaxValue = 100, Value = 100 };
            _playPauseButton.Click += PlayPauseButton_Click;
            volumeSlider.ValueChanged += VolumeSlider_ValueChanged;

            Content = new TableLayout(new TableRow(_playPauseButton), new TableRow(volumeSlider));
        }

        public void Stop()
        {
            _player.Stop();
        }

        private void PlayPauseButton_Click(object sender, System.EventArgs e)
        {
            if (_player.IsPlaying)
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

        private void VolumeSlider_ValueChanged(object sender, System.EventArgs e)
        {
            _player.Volume = ((Slider)sender).Value;
        }
    }
}
