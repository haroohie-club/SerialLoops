﻿using Eto.Forms;
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

            Application application = new(platform);
            MainForm mainForm = new();
            try
            {
                application.Run(mainForm);
            }
            catch (Exception ex)
            {
                mainForm.Log.LogError($"{ex.Message}\n\n{ex.StackTrace}");
            }
        }
    }
}
