using Eto.Forms;
using SerialLoops.Utility;
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

            Eto.Style.Add<Eto.Mac.Forms.ApplicationHandler>(null, handler =>
            {
                handler.EnableNativeCrashReport = true;
                handler.EnableNativeExceptionTranslation = true;
                handler.BadgeLabel = "Serial Loops";
            });

            new Application(platform).Run(new MainForm());
        }
    }
}
