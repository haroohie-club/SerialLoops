using System;
using HaruhiChokuretsuLib.Util;
#if !WINDOWS
using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
#endif
using NAudio.Wave;

namespace SerialLoops.Utility;

public class BgmVceMixer : IDisposable
{
    private ILogger _log;
#if WINDOWS
    private WaveOut _player;
#else
    private WaveOutSdl _player;
#endif
    public IWaveProvider WaveProvider { get; set; }
    public PlaybackState PlaybackState => _player.PlaybackState;

    public event EventHandler<StoppedEventArgs> PlaybackStopped
    {
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
    }

    public BgmVceMixer(IWaveProvider waveProvider, ILogger log)
    {
        _log = log;
        WaveProvider = waveProvider;
#if WINDOWS
        _player = new() { DeviceNumber = -1 };
        _player.Init(WaveProvider);
#else
        _player = new() { DesiredLatency = 10 };
#endif
    }

    public void Pause()
    {
        _player.Pause();
    }

    public void Play()
    {
        try
        {
#if !WINDOWS
            if (_player.PlaybackState == PlaybackState.Stopped)
            {
                _player.Init(WaveProvider);
            }
#endif
            _player.Play();
        }
        catch (Exception ex)
        {
            _log.LogWarning($"Failed to init wave provider due to exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void Stop()
    {
        _player.Stop();
#if !WINDOWS
        _player.Dispose();
#endif
    }

    public void Dispose() => _player?.Dispose();
}
