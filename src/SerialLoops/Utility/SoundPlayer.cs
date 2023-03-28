using Eto;
using Eto.Forms;
using NAudio.Wave;

namespace SerialLoops.Utility
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
        public PlaybackState PlaybackState
        {
            get => Handler.PlaybackState;
        }

        public void Initialize(IWaveProvider waveProvider) => Handler.Initialize(waveProvider);
        public void Play() => Handler.Play();
        public void Pause() => Handler.Pause();
        public void Stop() => Handler.Stop();

        public interface ISoundPlayer : IHandler
        {
            public IWaveProvider WaveProvider { get; set; }
            public PlaybackState PlaybackState { get; }

            public void Initialize(IWaveProvider waveProvider);
            public void Play();
            public void Pause();
            public void Stop();

        }
    }
}
