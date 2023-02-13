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
            new Application(Eto.Platforms.Wpf).Run(new MainForm());
        }
    }
}
