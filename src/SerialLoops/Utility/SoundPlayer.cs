using NAudio.Wave;

namespace SerialLoops.Utility
{
    public class SoundPlayer
    {
#if WINDOWS
        private WaveOut _player;
#else
        private ALWavePlayer _player;
#endif
        public IWaveProvider WaveProvider { get; set; }
        public PlaybackState PlaybackState => _player.PlaybackState;

        public SoundPlayer(IWaveProvider waveProvider)
        {
            WaveProvider = waveProvider;

#if WINDOWS
            _player = new() { DeviceNumber = -1 };
#else
            _player = new(new(), 8192);
            _player.PlaybackStopped += (sender, args) =>
            {
                if (args.Exception is null)
                {
                    _player.Dispose();
                    _player = new(new(), 8192);
                }
            };
#endif
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
