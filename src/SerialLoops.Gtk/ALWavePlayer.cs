using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SerialLoops.Gtk
{
    public class ALWavePlayer : IWavePlayer, IDisposable
    {
        private CancellationTokenSource _cancellationToken;
        private IWaveProvider _waveProvider;
        private int _audioBufferSize = 5;
        private int _bufferSize = 10240;
        private int[] _audioBuffers;
        private int _audioSource;
        private int _bufferBytes;
        private byte[] _buffer;

        public float Volume { get; set; }
        public PlaybackState PlaybackState { get; }
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public ALSourceState State => AL.GetSourceState(_audioSource);
        public int AudioBufferSize
        {
            get => _audioBufferSize;
            set
            {
                if (_audioBuffers is not null)
                {
                    Console.WriteLine("Not possible to change after initialization");
                    return;
                }

                _audioBufferSize = value;
            }
        }
        public int BufferSize
        {
            get => _bufferSize;
            set
            {
                if (_waveProvider is not null)
                {
                    Console.WriteLine("Not possible to change after initialization");
                    return;
                }

                _bufferSize = value;
            }
        }

        public WaveFormat OutputWaveFormat => _waveProvider.WaveFormat;

        public void Dispose()
        {
            if (_waveProvider is not null)
            {
                _waveProvider = default;
                AL.DeleteSource(_audioSource);
                AL.DeleteBuffers(_audioBuffers);
            }
        }

        public void Init(IWaveProvider waveProvider)
        {
            _waveProvider = waveProvider;
            _buffer = new byte[_bufferSize];

            _audioSource = AL.GenSource();
            _audioBuffers = AL.GenBuffers(_audioBufferSize);
        }

        public void Pause()
        {
            if (State == ALSourceState.Stopped)
            {
                return;
            }

            _cancellationToken?.Cancel();
            AL.SourcePause(_audioSource);
        }

        public async void Play()
        {
            if (State == ALSourceState.Stopped)
            {
                return;
            }

            if (_cancellationToken is null)
            {
                _cancellationToken = new();
                await PlayAsync();
            }
        }

        public void Stop()
        {
            if (State == ALSourceState.Stopped) 
            { 
                return;
            }

            if (_cancellationToken is not null)
            {
                _cancellationToken.Cancel();
                AL.SourceStop(_audioSource);
                return;
            }

            PlaybackStopped?.Invoke(this, new StoppedEventArgs());
        }

        public async Task PlayAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                AL.GetSource(_audioSource, ALGetSourcei.BuffersProcessed, out int completedBuffers);
                AL.GetSource(_audioSource, ALGetSourcei.BuffersQueued, out int queuedBuffers);

                var nextBuffer = _audioBufferSize - queuedBuffers + completedBuffers;
                if (nextBuffer > 0)
                {
                    nextBuffer = _audioBuffers[nextBuffer - 1];
                    if (completedBuffers > 0)
                    {
                        AL.SourceUnqueueBuffers(_audioSource, completedBuffers, ref nextBuffer);
                    }

                    WriteToAudioBuffer(nextBuffer);
                    AL.SourceQueueBuffers(_audioSource, 1, ref nextBuffer);
                }

                if (State != ALSourceState.Playing)
                {
                    AL.SourcePlay(_audioSource);
                }
                await Task.Delay(10);
            }

            _cancellationToken = null;
        }

        protected unsafe void WriteToAudioBuffer(int audioBuffer)
        {
            _bufferBytes = _waveProvider.Read(_buffer, 0, _buffer.Length);
            fixed (byte* ptr = _buffer)
            {
                AL.BufferData(audioBuffer, ParseFormat(_waveProvider.WaveFormat), ptr, _bufferBytes, _waveProvider.WaveFormat.SampleRate);
            }
        }

        public static ALFormat ParseFormat(WaveFormat format)
        {
            if (format.Channels == 2)
            {
                if (format.BitsPerSample == 32)
                {
                    return ALFormat.StereoFloat32Ext;
                }
                else if (format.BitsPerSample == 16)
                {
                    return ALFormat.Stereo16;
                }
                else if (format.BitsPerSample == 8)
                {
                    return ALFormat.Stereo8;
                }
            }
            else if (format.Channels == 1)
            {
                if (format.BitsPerSample == 32)
                {
                    return ALFormat.MonoFloat32Ext;
                }
                else if (format.BitsPerSample == 16)
                {
                    return ALFormat.Mono16;
                }
                else if (format.BitsPerSample == 8)
                {
                    return ALFormat.Mono8;
                }
            }

            throw new FormatException("Cannot translate WaveFormat.");
        }
    }
}
