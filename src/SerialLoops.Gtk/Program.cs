using Eto.Forms;
using Eto.Forms.Controls.SkiaSharp.GTK;
using Eto.Forms.Controls.SkiaSharp.Shared;
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
            platform.Add<SKControl.ISKControl>(() => new SKControlHandler());
            platform.Add<SKGLControl.ISKGLControl>(() => new SKGLControlHandler());

            new Application(platform).Run(new MainForm());
        }
    }
}
