using Eto.Forms;
using Eto.IO;
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

            new Application(platform).Run(new MainForm());
        }
    }
}
