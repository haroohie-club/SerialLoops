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

            Eto.Style.Add<Eto.GtkSharp.Forms.ApplicationHandler>(null, handler =>
            {
                handler.BadgeLabel = "Serial Loops";
            });

            new Application(platform).Run(new MainForm());
        }
    }
}
