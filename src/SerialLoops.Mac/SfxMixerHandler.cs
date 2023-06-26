using Eto.Mac.Forms.Controls;
using MonoMac.AppKit;
using NAudio.Wave;
using SerialLoops.Utility;

namespace SerialLoops.Mac
{
    public class SfxMixerHandler : MacControl<NSControl, SfxMixer, SfxMixer.ICallback>, SfxMixer.ISfxMixer
    {
        private ALWavePlayer _player;

        public IWavePlayer Player => _player;

        public SfxMixerHandler()
        {
            Control = new NSControl();
            _player = new(new(), 16384);
        }
    }
}
