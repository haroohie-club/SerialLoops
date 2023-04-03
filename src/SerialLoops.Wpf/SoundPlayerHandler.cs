using Eto.Wpf.Forms;
using NAudio.Wave;
using SerialLoops.Utility;
using System.Windows.Controls;

namespace SerialLoops.Wpf
{
    public class SoundPlayerHandler : WpfControl<Button, SoundPlayer, SoundPlayer.ICallback>, SoundPlayer.ISoundPlayer
    {
        private WaveOut _player;
        public IWaveProvider WaveProvider { get; set; }
        public PlaybackState PlaybackState => _player.PlaybackState;

        public SoundPlayerHandler()
        {
            Control = new Button();
        }

        public void Initialize(IWaveProvider waveProvider)
        {
            WaveProvider = waveProvider;

            _player = new() { DeviceNumber = -1 };
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
