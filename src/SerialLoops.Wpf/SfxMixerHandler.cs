using Eto.Wpf.Forms;
using NAudio.Wave;
using SerialLoops.Utility;
using System.Windows.Controls;

namespace SerialLoops.Wpf
{
    public class SfxMixerHandler : WpfControl<Control, SfxMixer, SfxMixer.ICallback>, SfxMixer.ISfxMixer
    {
        private WaveOut _player;

        public IWavePlayer Player => _player;

        public SfxMixerHandler()
        {
            Control = new Control();
            _player = new() { DeviceNumber = -1 };
        }
    }
}
