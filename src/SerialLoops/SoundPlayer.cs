using Eto;
using Eto.Forms;
using NAudio.Wave;

namespace SerialLoops
{
    [Handler(typeof(ISoundPlayer))]
    public class SoundPlayer : Control
    {
        public new ISoundPlayer Handler => (ISoundPlayer)base.Handler;

        public IWaveProvider WaveProvider
        {
            get => Handler.WaveProvider;
            set => Handler.WaveProvider = value;
        }
        public bool IsPlaying
        {
            get => Handler.IsPlaying;
            set => Handler.IsPlaying = value;
        }

        public void Play() => Handler.Play();
        public void Pause() => Handler.Pause();
        public void Stop() => Handler.Stop();


        public interface ISoundPlayer : IHandler
        {
            public IWaveProvider WaveProvider { get; set; }
            public bool IsPlaying { get; set; }

            public void Play();
            public void Pause();
            public void Stop();

        }
    }
}
