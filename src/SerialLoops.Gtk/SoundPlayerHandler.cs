using Eto.GtkSharp.Forms;
using Gtk;
using NAudio.Wave;
using SerialLoops.Utility;

namespace SerialLoops.Gtk
{
    public class SoundPlayerHandler : GtkControl<Button, SoundPlayer, SoundPlayer.ICallback>, SoundPlayer.ISoundPlayer
    {
        private ALWavePlayer _player;
        public IWaveProvider WaveProvider { get; set; }

        public PlaybackState PlaybackState => _player.PlaybackState;

        public SoundPlayerHandler()
        {
            Control = new Button();
        }

        public void Initialize(IWaveProvider waveProvider)
        {
            WaveProvider = waveProvider;
            _player = new(new(), 8192);
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
            // AL has a static player, so if we stop it we'll throw errors
            _player.Pause();
        }
    }
}
