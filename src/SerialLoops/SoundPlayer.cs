using Eto;
using Eto.Forms;
using NAudio.Wave;

namespace SerialLoops
{
    [Handler(typeof(ISoundPlayer))]
    public class SoundPlayer : Control
    {
        new ISoundPlayer Handler => (ISoundPlayer)base.Handler;

        public IWaveProvider WaveProvider
        {
            get => Handler.WaveProvider;
            set => Handler.WaveProvider = value;
        }
        public bool IsPlaying
        {
            get => Handler.IsPlaying;
        }

        public void Initialize(IWaveProvider waveProvider) => Handler.Initialize(waveProvider);
        public void Play() => Handler.Play();
        public void Pause() => Handler.Pause();
        public void Stop() => Handler.Stop();

        public interface ISoundPlayer : IHandler
        {
            public IWaveProvider WaveProvider { get; set; }
            public bool IsPlaying { get; }

            public void Initialize(IWaveProvider waveProvider);
            public void Play();
            public void Pause();
            public void Stop();

        }
    }
}
