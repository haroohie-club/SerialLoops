using Eto.GtkSharp.Forms;
using Gtk;
using NAudio.Wave;
using SerialLoops.Utility;

namespace SerialLoops.Gtk
{
    public class SfxMixerHandler : GtkControl<Button, SfxMixer, SfxMixer.ICallback>, SfxMixer.ISfxMixer
    {
        private ALWavePlayer _player;

        public IWavePlayer Player => _player;

        public SfxMixerHandler()
        {
            Control = new Button();
            _player = new(new(), 8192);
        }
    }
}
