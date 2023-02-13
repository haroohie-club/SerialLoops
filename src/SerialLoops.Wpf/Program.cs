using Eto.Forms;
using SerialLoops;
using System;

namespace TestEto.Wpf
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Eto.Wpf.Platform();
            

            new Application(Eto.Platforms.Wpf).Run(new MainForm());
        }
    }
}
