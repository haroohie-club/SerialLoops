using NAudio.Wave;

namespace SerialLoops.Utility
{
    public class SfxMixer
    {
#if WINDOWS
        private WaveOut _player;
#else
        private ALWavePlayer _player;
#endif

        public IWavePlayer Player => _player;

        public SfxMixer()
        {
#if WINDOWS
            _player = new() { DesiredLatency = 100 };
#else
            _player = new(new(), 16384);
#endif
        }
    }
}
