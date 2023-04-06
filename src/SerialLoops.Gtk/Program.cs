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

            Eto.Style.Add<Window>(null, handler =>
            {
                handler.Icon = Icon.FromResource($"SerialLoops.AppIcon.png").WithSize(64, 64);
            });

            new Application(platform).Run(new MainForm());
        }
    }
}
