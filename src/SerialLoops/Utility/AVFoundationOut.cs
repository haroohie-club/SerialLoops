#if MACOS
using System;
using System.Buffers.Binary;
using System.Threading;
using AVFoundation;
using NAudio.Wave;

namespace SerialLoops.Utility;

public class AVFoundationOut : IWavePlayer
{
    public float Volume
    {
        get;
        set
        {
            field = (value < 0.0f) ? 0.0f : (value > 1.0f) ? 1.0f : value;
            if (_audioPlayerNode is not null)
            {
                _audioPlayerNode.Volume = _volume;
            }
        }
    }

    public int NumberOfBuffers { get; set; } = 4;

    public PlaybackState PlaybackState { get; private set; } = PlaybackState.Stopped;
    public WaveFormat OutputWaveFormat { get; private set; }
    public event EventHandler<StoppedEventArgs> PlaybackStopped;

    private AVAudioFormat? _sourceAudioFormat;
    private AVAudioFormat? _outputAudioFormat;
    private AVAudioConverter? _audioConverter;
    private AVAudioEngine? _audioEngine;
    private AVAudioPlayerNode? _audioPlayerNode;
    private AVAudioPcmBuffer[] _buffers = [];
    private SemaphoreSlim? _bufferSemaphore;
    private float _volume = 1.0f;
    private int _bufferIndex;
    private uint _bufferFrames;

    public void Init(IWaveProvider waveProvider)
    {
        OutputWaveFormat = waveProvider.WaveFormat;

        _sourceAudioFormat = new(AVAudioCommonFormat.PCMInt16, sampleRate, 2, false);
        _audioPlayerNode = new();
        _audioPlayerNode.Volume = _volume;
        _audioEngine = new();
        AVAudioFormat mainFormat = _audioEngine.MainMixerNode.GetBusOutputFormat(0);
        _outputAudioFormat = new(mainFormat.CommonFormat, sampleRate, mainFormat.ChannelCount, mainFormat.Interleaved);
        _audioConverter = new(_sourceAudioFormat, _outputAudioFormat);
        _audioEngine.AttachNode(_audioPlayerNode);
        _audioEngine.Connect(_audioPlayerNode, _audioEngine.MainMixerNode, _outputAudioFormat);

        _buffers = new AVAudioPcmBuffer[NumberOfBuffers];
        int bufferSize = 32768;
        _bufferFrames = (uint)(bufferSize / 4);
        for (int i = 0; i < NumberOfBuffers; i++)
        {
            _buffers[i] = new(_outputAudioFormat, _bufferFrames);
        }

        _bufferSemaphore = new(NumberOfBuffers, NumberOfBuffers);
    }

    public void Play()
    {
        if (PlaybackState == PlaybackState.Playing)
            return;

        PlaybackState = PlaybackState.Playing;
        if (!(_audioEngine?.Running ?? true))
        {
            _audioEngine?.StartAndReturnError(out _);
        }
        _audioPlayerNode?.Play();
    }

    public void Pause()
    {
        if (PlaybackState != PlaybackState.Playing)
            return;

        PlaybackState = PlaybackState.Paused;
        _audioPlayerNode?.Pause();
        _audioEngine?.Pause();
    }

    public void Stop()
    {
        if (PlaybackState == PlaybackState.Stopped)
            return;

        PlaybackState = PlaybackState.Stopped;
        _audioPlayerNode?.Stop();
        _audioEngine?.Stop();
    }

    public void Dispose()
    {

    }
}
#endif
