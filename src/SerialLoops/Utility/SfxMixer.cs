#if !WINDOWS
using NAudio.Sdl2;
#endif
using NAudio.Wave;

namespace SerialLoops.Utility;

public class SfxMixer
{
#if WINDOWS
    private WaveOut _player;
#else
    private WaveOutSdl _player;
#endif

    public IWavePlayer Player => _player;

    public SfxMixer()
    {
#if WINDOWS
        _player = new() { DesiredLatency = 100 };
#else
        _player = new() { DesiredLatency = 10 };
#endif

    }
}
