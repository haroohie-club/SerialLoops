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
    private readonly WaveOut _player;
#elif MACOS
    private readonly AVFoundationOut _player;
#else
#pragma warning disable CA1416
    private readonly AlsaOut _player;
#pragma warning restore CA1416
#endif

    public IWavePlayer Player => _player;

    public SfxMixer()
    {
#if WINDOWS
        _player = new() { BufferMilliseconds = 100 };
#elif MACOS
        _player = new();
#else
#pragma warning disable CA1416
        _player = new();
#pragma warning restore CA1416
#endif
    }

#pragma warning disable CA1416
    public void Dispose()
    {
        _player?.Dispose();
        GC.SuppressFinalize(this);
    }
#pragma warning restore CA1416
}
