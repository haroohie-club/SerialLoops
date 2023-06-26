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
            platform.Add<SfxMixer.ISfxMixer>(() => new SfxMixerHandler());

            Eto.Style.Add<Eto.Mac.Forms.ApplicationHandler>(null, handler =>
            {
                handler.EnableNativeCrashReport = true;
                handler.EnableNativeExceptionTranslation = true;
            });

            new Application(platform).Run(new MainForm());
        }
    }
}
