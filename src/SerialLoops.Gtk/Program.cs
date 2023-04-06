using Eto.Drawing;
using Eto.Forms;
using Eto.GtkSharp.Forms;
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

            platform.Add<LinuxTrayIndicatorHandler>(() => new()
            {
                Image = new Bitmap("Icons/AppIcon.png")
            });

            new Application(platform).Run(new MainForm());
        }
    }
}
