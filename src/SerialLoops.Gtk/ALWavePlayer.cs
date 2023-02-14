using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading.Tasks;

namespace SerialLoops.Gtk
{
    public class ALAudioContext : IDisposable
    {
        public ALDevice Device { get; private set; }
        public ALContext Context { get; private set; }

        public ALAudioContext()
        {
            Init();
        }

        private unsafe void Init()
        {
            Device = ALC.OpenDevice(null);
            Context = ALC.CreateContext(Device, (int*)null);
            ALC.MakeContextCurrent(Context);
        }

        ~ALAudioContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (Context != ALContext.Null)
            {
                ALC.MakeContextCurrent(ALContext.Null);
                ALC.DestroyContext(Context);
            }
            Context = ALContext.Null;

            if (Device != IntPtr.Zero)
            {
                ALC.CloseDevice(Device);
            }
            Device = ALDevice.Null;
        }
    }

    public class ALWavePlayer : IWavePlayer, IDisposable
    {
        private float _volume;
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
            }
        }

        public PlaybackState PlaybackState { get; private set; }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        private ALAudioContext Context { get; }
        public int BufferSize { get; }

        private IWaveProvider WaveProvider;

        private int _source;
        private int _nextBuffer;
        private int _otherBuffer;

        private byte[] _buffer;
        private Accumulator _accumulator;

        private System.Threading.ManualResetEventSlim _signaller;

        private System.Threading.CancellationTokenSource _playerCanceller;
        private Task Player;

        public WaveFormat OutputWaveFormat => WaveProvider.WaveFormat;

        public ALWavePlayer(ALAudioContext context, int bufferSize)
        {
            Context = context;
            BufferSize = bufferSize;
        }

        public unsafe void Init(IWaveProvider waveProvider)
        {
            WaveProvider = waveProvider;

            AL.GenSources(1, ref _source);
            AL.GenBuffers(1, ref _nextBuffer);
            AL.GenBuffers(1, ref _otherBuffer);

            _buffer = new byte[BufferSize];
            _accumulator = new Accumulator(waveProvider, _buffer);

            _signaller = new System.Threading.ManualResetEventSlim(false);
            PlaybackState = PlaybackState.Paused;
        }

        public void Pause()
        {
            if (PlaybackState == PlaybackState.Stopped)
                throw new InvalidOperationException("Stopped");

            PlaybackState = PlaybackState.Paused;
            _playerCanceller?.Cancel();
            _playerCanceller = null;
            AL.SourcePause(_source);
        }

        public void Play()
        {
            if (PlaybackState == PlaybackState.Stopped)
                throw new InvalidOperationException("Stopped");

            PlaybackState = PlaybackState.Playing;
            if (_playerCanceller == null)
            {
                _playerCanceller = new System.Threading.CancellationTokenSource();
                Player = PlayLoop(_playerCanceller.Token).ContinueWith(PlayerStopped);
            }
        }

        private void PlayerStopped(Task t)
        {
            PlaybackStopped?.Invoke(this, new StoppedEventArgs(t?.Exception));
        }

        public void Stop()
        {
            if (PlaybackState == PlaybackState.Stopped)
                throw new InvalidOperationException("Already stopped");

            PlaybackState = PlaybackState.Stopped;
            if (_playerCanceller != null)
            {
                _playerCanceller?.Cancel();
                _playerCanceller = null;
                AL.SourceStop(_source);
            }
            else
            {
                PlaybackStopped?.Invoke(this, new StoppedEventArgs());
            }
        }

        private async Task PlayLoop(System.Threading.CancellationToken ct)
        {
            AL.SourcePlay(_source);
            await Task.Yield();

            while (true)
            {
                AL.GetSource(_source, ALGetSourcei.BuffersQueued, out int queued);
                AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out int processed);
                AL.GetSource(_source, ALGetSourcei.SourceState, out int state);

                if ((ALSourceState)state != ALSourceState.Playing)
                {
                    AL.SourcePlay(_source);
                }

                if (processed == 0 && queued == 2)
                {
                    await Task.Delay(1);
                    continue;
                }

                if (processed > 0)
                {
                    AL.SourceUnqueueBuffers(_source, processed);
                }

                var notFinished = await _accumulator.Accumulate(ct);
                _accumulator.Reset();

                if (!notFinished)
                {
                    return;
                }

                AL.BufferData(_nextBuffer, TranslateFormat(WaveProvider.WaveFormat), _buffer, WaveProvider.WaveFormat.SampleRate);
                AL.SourceQueueBuffer(_source, _nextBuffer);

                (_nextBuffer, _otherBuffer) = (_otherBuffer, _nextBuffer);
            }
        }

        ~ALWavePlayer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            AL.DeleteSource(_source);
            AL.DeleteBuffer(_nextBuffer);
            AL.DeleteBuffer(_otherBuffer);
        }

        public static ALFormat TranslateFormat(WaveFormat format)
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

    public class Accumulator
    {
        public Accumulator(IWaveProvider provider, byte[] buffer)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            Position = 0;
        }

        public IWaveProvider Provider { get; }
        public byte[] Buffer { get; }
        public int Position { get; private set; }
        //private object Locker = new();

        public async Task<bool> Accumulate(System.Threading.CancellationToken ct)
        {
            if (Position == Buffer.Length)
                return true;

            await Task.Yield();

            while (Position != Buffer.Length)
            {
                if (ct.IsCancellationRequested)
                    throw new TaskCanceledException();
                var read = Provider.Read(Buffer, Position, Buffer.Length - Position);

                if (read == 0)
                    return false;

                Position += read;
            }

            return true;
        }

        public void Reset()
        {
            Position = 0;
        }
    }
}