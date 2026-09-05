using System;
#if MACOS
#elif !WINDOWS
using NAudio.Wave.Alsa;
#endif
using NAudio.Wave;

namespace SerialLoops.Utility;

public class SfxMixer : IDisposable
{
#if WINDOWS
    private WaveOut _player;
#elif MACOS
#else
    private AlsaOut _player;
#endif

    public IWavePlayer Player => _player;

    public SfxMixer()
    {
#if WINDOWS
        _player = new() { BufferMilliseconds = 100 };
#elif MACOS

#else
        _player = new();
#endif
    }

    public void Dispose() => _player?.Dispose();
}
