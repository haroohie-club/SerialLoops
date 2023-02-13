using Eto.Forms;
using SerialLoops;
using System;

namespace TestEto.Gtk
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Eto.Platforms.Gtk).Run(new MainForm());
        }
    }
}
