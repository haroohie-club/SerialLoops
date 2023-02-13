using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading.Tasks;

namespace SerialLoops.Gtk
{
    public class SineProvider : ISampleProvider
    {
        public SineProvider(WaveFormat waveFormat, double frequency, double amplitude, double duration)
        {
            if (waveFormat.Channels != 1)
                throw new ArgumentException("Must be a mono wave format.");

            Time = 0;
            WaveFormat = waveFormat;
            Frequency = frequency;
            Amplitude = amplitude;
            Duration = duration;
        }

        public WaveFormat WaveFormat { get; }
        public double Time { get; set; }
        public double Frequency { get; set; }
        public double Amplitude { get; set; }
        public double Duration { get; set; }

        public int Read(float[] buffer, int offset, int count)
        {
            var dt = 1.0 / WaveFormat.SampleRate;
            for (int i = 0; i < count; i++)
            {
                if (Time >= Duration)
                {
                    Time = Duration;
                    return i;
                }

                buffer[offset + i] = (float)(Math.Sin(Frequency * Math.PI * 2.0 * Time) * Amplitude);
                Time += dt;
            }

            return count;
        }
    }

    public class TkAudioContext : IDisposable
    {
        public ALDevice Device { get; private set; }
        public ALContext Context { get; private set; }

        public TkAudioContext()
        {
            Init();
        }

        private unsafe void Init()
        {
            Device = ALC.OpenDevice(null);
            Context = ALC.CreateContext(Device, (int*)null);
            ALC.MakeContextCurrent(Context);
        }

        ~TkAudioContext()
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

    public class TkWavePlayer : IWavePlayer, IDisposable
    {
        private float volume;
        public float Volume
        {
            get => volume;
            set
            {
                volume = value;
            }
        }

        public PlaybackState PlaybackState { get; }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        private TkAudioContext Context { get; }
        public int BufferSize { get; }

        private IWaveProvider WaveProvider;

        private int Source;
        private int NextBuffer;
        private int OtherBuffer;

        private byte[] Buffer;
        private Accumulator Accumulator;

        private System.Threading.ManualResetEventSlim Signaller;

        private System.Threading.CancellationTokenSource PlayerCanceller;
        private Task Player;
        public bool Paused { get; private set; } = false;
        public bool Stopped { get; private set; } = false;

        public WaveFormat OutputWaveFormat => WaveProvider.WaveFormat;

        public TkWavePlayer(TkAudioContext context, int bufferSize)
        {
            Context = context;
            BufferSize = bufferSize;
        }

        public unsafe void Init(IWaveProvider waveProvider)
        {
            WaveProvider = waveProvider;

            AL.GenSources(1, ref Source);
            AL.GenBuffers(1, ref NextBuffer);
            AL.GenBuffers(1, ref OtherBuffer);

            Buffer = new byte[BufferSize];
            Accumulator = new Accumulator(waveProvider, Buffer);

            Signaller = new System.Threading.ManualResetEventSlim(false);
        }

        public void Pause()
        {
            if (Stopped)
                throw new InvalidOperationException("Stopped");

            Paused = true;
            PlayerCanceller?.Cancel();
            PlayerCanceller = null;
            AL.SourcePause(Source);
        }

        public void Play()
        {
            if (Stopped)
                throw new InvalidOperationException("Stopped");

            Paused = false;
            if (PlayerCanceller == null)
            {
                PlayerCanceller = new System.Threading.CancellationTokenSource();
                Player = PlayLoop(PlayerCanceller.Token).ContinueWith(PlayerStopped);
            }
        }

        private void PlayerStopped(Task t)
        {
            if (!Paused)
            {
                PlaybackStopped?.Invoke(this, new StoppedEventArgs(t?.Exception));
            }
        }

        public void Stop()
        {
            if (Stopped)
                throw new InvalidOperationException("Already stopped");

            Paused = false;
            if (PlayerCanceller != null)
            {
                PlayerCanceller?.Cancel();
                PlayerCanceller = null;
                AL.SourceStop(Source);
            }
            else
            {
                PlaybackStopped?.Invoke(this, new StoppedEventArgs());
            }
        }

        private async Task PlayLoop(System.Threading.CancellationToken ct)
        {
            AL.SourcePlay(Source);
            await Task.Yield();

        again:
            AL.GetSource(Source, ALGetSourcei.BuffersQueued, out int queued);
            AL.GetSource(Source, ALGetSourcei.BuffersProcessed, out int processed);
            AL.GetSource(Source, ALGetSourcei.SourceState, out int state);

            if ((ALSourceState)state != ALSourceState.Playing)
            {
                AL.SourcePlay(Source);
            }

            if (processed == 0 && queued == 2)
            {
                await Task.Delay(1);
                goto again;
            }

            if (processed > 0)
            {
                AL.SourceUnqueueBuffers(Source, processed);
            }

            var notFinished = await Accumulator.Accumulate(ct);
            Accumulator.Reset();

            if (!notFinished)
            {
                return;
            }

            AL.BufferData(NextBuffer, TranslateFormat(WaveProvider.WaveFormat), Buffer, WaveProvider.WaveFormat.SampleRate);
            AL.SourceQueueBuffer(Source, NextBuffer);

            (NextBuffer, OtherBuffer) = (OtherBuffer, NextBuffer);

            goto again;
        }

        ~TkWavePlayer()
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
            AL.DeleteSource(Source);
            AL.DeleteBuffer(NextBuffer);
            AL.DeleteBuffer(OtherBuffer);
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
        private object Locker = new object();

        public async Task<bool> Accumulate(System.Threading.CancellationToken ct)
        {
            if (Position == Buffer.Length)
                return true;

            await Task.Yield();

            lock (Locker)
            {
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
        }

        public void Reset()
        {
            Position = 0;
        }
    }
}
