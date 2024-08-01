using System;
using NAudio.Wave;

namespace SerialLoops.Utility
{
    public class SoundPlayer
    {
#if WINDOWS
        private WaveOut _player;
#else
        private ALWavePlayer _player;
        private static readonly ALAudioContext _context = new();
#endif
        public IWaveProvider WaveProvider { get; set; }
        public PlaybackState PlaybackState => _player.PlaybackState;

        public event EventHandler<StoppedEventArgs> PlaybackStopped
        {
            add => _player.PlaybackStopped += value;
            remove => _player.PlaybackStopped -= value;
        }

        public SoundPlayer(IWaveProvider waveProvider)
        {
            WaveProvider = waveProvider;

#if WINDOWS
            _player = new() { DeviceNumber = -1 };
#else
            _player = new(_context, 8192);
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
#if WINDOWS
            _player.Stop();
#else
            // AL has a static player, so if we stop it we'll throw errors
            _player.Pause();
#endif
        }
    }
}
