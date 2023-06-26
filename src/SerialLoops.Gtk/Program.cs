using Eto.Forms;
using SerialLoops.Utility;
using System;

namespace SerialLoops.Gtk
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Eto.GtkSharp.Platform();
            platform.Add<SoundPlayer.ISoundPlayer>(() => new SoundPlayerHandler());
            platform.Add<SfxMixer.ISfxMixer>(() => new SfxMixerHandler());

            new Application(platform).Run(new MainForm());
        }
    }
}
