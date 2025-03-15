using System;
#if !WINDOWS
using NAudio.Sdl2;
#endif
using NAudio.Wave;

namespace SerialLoops.Utility;

public class BgmVceMixer : IDisposable
{
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

    public BgmVceMixer(IWaveProvider waveProvider)
    {
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
#if !WINDOWS
        _player.Init(WaveProvider);
#endif
        _player.Play();
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
