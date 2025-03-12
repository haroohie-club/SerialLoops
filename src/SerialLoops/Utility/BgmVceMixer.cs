using System;
#if !WINDOWS
using NAudio.Sdl2;
#endif
using NAudio.Wave;

namespace SerialLoops.Utility;

public class BgmVceMixer
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
        add => _player.PlaybackStopped += value;
        remove => _player.PlaybackStopped -= value;
    }

    public BgmVceMixer(IWaveProvider waveProvider)
    {
        WaveProvider = waveProvider;

#if WINDOWS
        _player = new() { DeviceNumber = -1 };
#else
        _player = new() { DesiredLatency = 10 };
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
