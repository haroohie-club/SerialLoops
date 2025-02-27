#if !WINDOWS
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;
using Nito.AsyncEx;
using PortAudioSharp;
using PAStream = PortAudioSharp.Stream;

namespace SerialLoops.Utility;

// Borrowed from https://github.com/haiyaku365/StimmingSignalGenerator/blob/master/StimmingSignalGenerator/NAudio/PortAudio/PortAudioWavePlayer.cs
// Licensed under MIT License (https://github.com/haiyaku365/StimmingSignalGenerator/tree/master?tab=MIT-1-ov-file#readme)
// Copyright (c) 2020 haiyaku365
public class PortAudioWavePlayer : IWavePlayer
{
    public float Volume
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public PlaybackState PlaybackState { get; private set; }

    public event EventHandler<StoppedEventArgs> PlaybackStopped;

    public int Latency { get; }

    public WaveFormat OutputWaveFormat => _sourceProvider.WaveFormat;
    private int _deviceIndex;
    private DeviceInfo _deviceInfo;

    private IWaveProvider _sourceProvider;
    private int _bufferSizeByte;
    private CircularBuffer _sourceBuffer;
    private byte[] _bufferWrite;
    private byte[] _bufferRead;
    private byte[] _bufferWriteLastestBlock;
    private readonly AsyncAutoResetEvent sourceBufferDequeuedEvent;

    private PAStream _stream;

    public PortAudioWavePlayer(int latency)
    {
        PortAudio.LoadNativeLibrary();
        PortAudio.Initialize();
        for (int i = 0; i < PortAudio.DeviceCount; i++)
        {
            DeviceInfo info = PortAudio.GetDeviceInfo(i);
        }
        _deviceIndex = PortAudio.DefaultOutputDevice;
        _deviceInfo = PortAudio.GetDeviceInfo(_deviceIndex);
        Latency = latency;
        sourceBufferDequeuedEvent = new(false);
    }

    public void Init(IWaveProvider waveProvider)
    {
        _sourceProvider = waveProvider;
        _bufferSizeByte = OutputWaveFormat.ConvertLatencyToByteSize(Latency);
        _sourceBuffer = new(_bufferSizeByte);
        _bufferWrite = new byte[_bufferSizeByte];
        _bufferRead = new byte[_bufferSizeByte];
        _bufferWriteLastestBlock = new byte[OutputWaveFormat.BlockAlign];

        var param = new StreamParameters
        {
            device = _deviceIndex,
            channelCount = OutputWaveFormat.Channels,
            sampleFormat = SampleFormat.Int16,
            suggestedLatency = _deviceInfo.defaultLowInputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero,
        };

        StreamCallbackResult callback(
            IntPtr input, IntPtr output,
            uint frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags,
            IntPtr userData
        )
        {
            int cnt = (int)frameCount * OutputWaveFormat.BlockAlign;
            int byteReadCnt = _sourceBuffer.Read(_bufferWrite, 0, cnt);
            sourceBufferDequeuedEvent.Set();

            if (byteReadCnt >= OutputWaveFormat.BlockAlign)
            {
                // Copy latest data to use when not enough buffer.
                Array.Copy(
                    _bufferWrite, byteReadCnt - OutputWaveFormat.BlockAlign,
                    _bufferWriteLastestBlock, 0, OutputWaveFormat.BlockAlign);
            }

            while (byteReadCnt < cnt)
            {
                // When running out of buffer data (Latency too low).
                // Fill the rest of buffer with latest data
                // so wave does not jump.
                _bufferWrite[byteReadCnt] = _bufferWriteLastestBlock[byteReadCnt % OutputWaveFormat.BlockAlign];
                byteReadCnt++;
            }

            Marshal.Copy(_bufferWrite, 0, output, cnt);
            return StreamCallbackResult.Continue;
        }

        _stream = new(
            inParams: null, outParams: param, sampleRate: OutputWaveFormat.SampleRate,
            framesPerBuffer: 0,
            streamFlags: StreamFlags.PrimeOutputBuffersUsingStreamCallback,
            callback: callback,
            userData: IntPtr.Zero
        );
    }

    private readonly CancellationTokenSource _fillSourceBufferWorkerCts = new();
    private Task _fillSourceBufferWorker;

    private async Task FillSourceBufferTaskAsync()
    {
        _fillSourceBufferWorkerCts.TryReset();
        while (!_fillSourceBufferWorkerCts.Token.IsCancellationRequested)
        {
            var bufferSpace = _sourceBuffer.MaxLength - _sourceBuffer.Count;
            if (bufferSpace > 0)
            {
                FillSourceBuffer(bufferSpace);
            }

            await sourceBufferDequeuedEvent.WaitAsync(_fillSourceBufferWorkerCts.Token);
        }
    }

    private void FillSourceBuffer(int bufferSpace)
    {
        _sourceProvider.Read(_bufferRead, 0, bufferSpace);
        _sourceBuffer.Write(_bufferRead, 0, bufferSpace);
    }

    public void Pause()
    {
        Stop();
    }

    public void Play()
    {
        if (PlaybackState == PlaybackState.Playing)
        {
            return;
        }

        FillSourceBuffer(_sourceBuffer.MaxLength);
        _fillSourceBufferWorker = Task.Factory.StartNew(
            function: FillSourceBufferTaskAsync,
            cancellationToken: CancellationToken.None,
            creationOptions:
            TaskCreationOptions.RunContinuationsAsynchronously |
            TaskCreationOptions.LongRunning,
            scheduler: TaskScheduler.Default);
        _stream.Start();
        PlaybackState = PlaybackState.Playing;
    }

    public void Stop()
    {
        if (PlaybackState == PlaybackState.Stopped)
        {
            return;
        }

        if (PlaybackState != PlaybackState.Paused)
        {
            _stream.Stop();
        }
        _fillSourceBufferWorkerCts.Cancel();
        while (!_stream.IsStopped && _fillSourceBufferWorker.Status != TaskStatus.Running)
        {
            Thread.Sleep(30);
        }
        _fillSourceBufferWorker.Dispose();
        PlaybackState = PlaybackState.Stopped;
        PlaybackStopped?.Invoke(this, new(null));
    }

    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                _stream.Close();
                _stream.Dispose();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            Array.Clear(_bufferWrite);
            _bufferWrite = null;
            Array.Clear(_bufferRead);
            _bufferRead = null;
            _disposedValue = true;
        }
    }

    // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~PortAudioWavePlayer()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
#endif
