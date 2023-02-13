using Eto.Forms;
using System;

namespace SerialLoops.Wpf
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Eto.Wpf.Platform();
            platform.Add<SoundPlayer.ISoundPlayer>(() => new SoundPlayerHandler());

            new Application(platform).Run(new MainForm());
        }
    }
}
