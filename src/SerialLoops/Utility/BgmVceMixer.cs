using System;
using HaruhiChokuretsuLib.Util;
#if MACOS
#elif !WINDOWS
using NAudio.Wave.Alsa;
#endif
using NAudio.Wave;

namespace SerialLoops.Utility;

public class BgmVceMixer : IDisposable
{
    private readonly ILogger _log;
#if WINDOWS
    private readonly WaveOut _player;
#elif MACOS

#else
#pragma warning disable CA1416
    private readonly AlsaOut _player;
#pragma warning restore CA1416
#endif
    public IWaveProvider WaveProvider { get; set; }
#pragma warning disable CA1416
    public PlaybackState PlaybackState => _player.PlaybackState;
#pragma warning restore CA1416

    public event EventHandler<StoppedEventArgs> PlaybackStopped
    {
#pragma warning disable CA1416
        add
        {
            if (_player is not null)
            {
                _player.PlaybackStopped += value;
            }
        }
        remove
        {
            if (_player is not null)
            {
                _player.PlaybackStopped -= value;
            }
        }
#pragma warning restore CA1416
    }

    public BgmVceMixer(IWaveProvider waveProvider, ILogger log)
    {
        _log = log;
        WaveProvider = waveProvider;
#if WINDOWS
        _player = new() { DeviceNumber = -1 };
        _player.Init(WaveProvider);
#elif MACOS
#else
#pragma warning disable CA1416
        _player = new();
        _player.Init(WaveProvider);
#pragma warning restore CA1416
#endif
    }

    public void Pause()
    {
#pragma warning disable CA1416
        _player.Pause();
#pragma warning restore CA1416
    }

    public void Play()
    {
        try
        {
#pragma warning disable CA1416
            _player.Play();
#pragma warning restore CA1416
        }
        catch (Exception ex)
        {
            _log.LogWarning($"Failed to init wave provider due to exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void Stop()
    {
#pragma warning disable CA1416
        _player.Stop();
#pragma warning restore CA1416
    }

#pragma warning disable CA1416
    public void Dispose()
    {
        _player?.Dispose();
        GC.SuppressFinalize(this);
    }
#pragma warning restore CA1416
}
