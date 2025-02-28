using NAudio.Sdl2;
using NAudio.Wave;

namespace SerialLoops.Utility;

public class SfxMixer
{
#if WINDOWS
    private WaveOut _player;
#elif LINUX
    private PortAudioWavePlayer _player;
#else
    private WaveOutSdl _player;
#endif

    public IWavePlayer Player => _player;

    public SfxMixer()
    {
#if WINDOWS
        _player = new() { DesiredLatency = 100 };
#elif LINUX
        _player = new(4096);
#else
        _player = new() { DesiredLatency = 10 };
#endif

    }
}
