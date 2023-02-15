using Eto;
using Eto.Forms;
using System;

namespace SerialLoops.Test.Wpf
{

    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Platform.Detect).Run(new MainForm());
        }
    }
}
