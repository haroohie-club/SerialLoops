using Eto.Forms;
using Eto.Forms.Controls.SkiaSharp.Mac;
using Eto.Forms.Controls.SkiaSharp.Shared;
using Forms.Controls.SkiaSharp.Mac;
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
            platform.Add<SKControl.ISKControl>(() => new SKControlHandler());
            platform.Add<SKGLControl.ISKGLControl>(() => new SKGLControlHandler());

            Eto.Style.Add<Eto.Mac.Forms.ApplicationHandler>(null, handler =>
            {
                handler.EnableNativeCrashReport = true;
                handler.EnableNativeExceptionTranslation = true;
            });

            new Application(platform).Run(new MainForm());
        }
    }
}
