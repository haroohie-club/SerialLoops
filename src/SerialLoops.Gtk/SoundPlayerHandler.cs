using Eto.GtkSharp.Forms;
using Gtk;
using NAudio.Wave;
using System;

namespace SerialLoops.Gtk
{
    public class SoundPlayerHandler : GtkControl<Button, SoundPlayer, SoundPlayer.ICallback>, SoundPlayer.ISoundPlayer
    {
        private TkWavePlayer _player;
        public IWaveProvider WaveProvider { get; set; }

        public PlaybackState PlaybackState => _player.PlaybackState;

        public SoundPlayerHandler()
        {
            Control = new Button();
        }

        public void Initialize(IWaveProvider waveProvider)
        {
            _player = new(new(), 4096);
            _player.Init(WaveProvider);
        }

        public void Pause()
        {
            _player.Pause();
        }

        public void Play()
        {
            _player.Play();
        }

        public void Stop()
        {
            _player.Stop();
        }
    }
}
