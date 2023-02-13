using Eto.Forms;
using System;

namespace SerialLoops.Mac
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Eto.Mac.Platform();
            platform.Add<SoundPlayer.ISoundPlayer>(() => new SoundPlayerHandler());

            new Application(platform).Run(new MainForm());
        }
    }
}
