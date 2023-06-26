using Eto;
using Eto.Forms;
using NAudio.Wave;

namespace SerialLoops.Utility
{
    [Handler(typeof(ISfxMixer))]
    public class SfxMixer : Control
    {
        new ISfxMixer Handler => (ISfxMixer)base.Handler;

        public IWavePlayer WavePlayer
        {
            get => Handler.Player;
        }

        public interface ISfxMixer
        {
            public IWavePlayer Player { get; }
        }
    }
}
