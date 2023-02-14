using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SerialLoops.Gtk
{
    public class ALWavePlayer : IWavePlayer, IDisposable
    {
        public const float SpeedOfSound = 343.3f;
        public const float DoplerFactor = 1f;

        public float Volume { get; set; }
        public PlaybackState PlaybackState { get; }
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public ALSourceState State => AL.GetSourceState(sourceID);
        public int AudioBufferSize
        {
            get => _audioBufferSize;
            set
            {
                _audioBufferSize = value;
            }
        }
        public int BufferSize
        {
            get => _bufferSize;
            set
            {
                _bufferSize = value;
            }
        }

        private float _pitch = 1;
        public float pitch
        {
            get => _pitch;
            set
            {
                _pitch = value;
                AL.Source(sourceID, ALSourcef.Pitch, _gain);
            }
        }

        private float _gain = 1;
        public float gain
        {
            get => _gain;
            set
            {
                _gain = value;
                AL.Source(sourceID, ALSourcef.Gain, _gain);
            }
        }

        private float _maxDistance = 10;
        private float maxDistance
        {
            get => _maxDistance;
            set
            {
                _maxDistance = value;
                AL.Source(sourceID, ALSourcef.MaxDistance, _maxDistance);
            }
        }

        private bool _looping = false;
        public bool looping
        {
            get => _looping;
            set
            {
                _looping = value;
                AL.Source(sourceID, ALSourceb.Looping, value);
            }
        }

        public WaveFormat OutputWaveFormat => _waveProvider.WaveFormat;

        public void Dispose()
        {
            if (_waveProvider is not null)
            {
                _waveProvider = default;
                AL.DeleteSource(sourceID);
                AL.DeleteBuffers(_audioBuffers);
            }
            GC.SuppressFinalize(this);
        }

        public void Init(IWaveProvider waveProvider)
        {
            _waveProvider = waveProvider;
            _buffer = new byte[_bufferSize];

            sourceID = AL.GenSource();
            _audioBuffers = AL.GenBuffers(_audioBufferSize);
        }

        public void Pause()
        {
            if (State == ALSourceState.Stopped) return;
            AL.SourcePause(sourceID);
            _cancellationToken.Cancel();
        }

        public async void Play()
        {
            if (_cancellationToken is null)
            {
                _cancellationToken = new();
                await PlayAsync();
            }
        }

        public void Stop()
        {
            if (State == ALSourceState.Stopped) return;

            AL.SourceStop(sourceID);
            _cancellationToken.Cancel();

            PlaybackStopped?.Invoke(this, new StoppedEventArgs());
        }

        public async Task PlayAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                AL.GetSource(sourceID, ALGetSourcei.BuffersProcessed, out int completedBuffers);
                AL.GetSource(sourceID, ALGetSourcei.BuffersQueued, out int queuedBuffers);

                var nextBuffer = _audioBufferSize - queuedBuffers + completedBuffers;
                if (nextBuffer > 0)
                {
                    nextBuffer = _audioBuffers[nextBuffer - 1];
                    if (completedBuffers > 0)
                    {
                        AL.SourceUnqueueBuffers(sourceID, completedBuffers, ref nextBuffer);
                    }

                    WriteToAudioBuffer(nextBuffer);
                    AL.SourceQueueBuffers(sourceID, 1, ref nextBuffer);
                }

                if (State != ALSourceState.Playing)
                {
                    AL.SourcePlay(sourceID);
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

        private CancellationTokenSource _cancellationToken;
        private IWaveProvider _waveProvider;
        private int _audioBufferSize = 5;
        private int _bufferSize = 10240;
        private int[] _audioBuffers;
        private int sourceID;
        private int _bufferBytes;
        private byte[] _buffer;
    }
}