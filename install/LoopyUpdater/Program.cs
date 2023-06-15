using System.Runtime.InteropServices;

namespace LoopyUpdater
{
    internal class Program
    {

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid arguments. Usage: LoopyUpdater <url>");
                return;
            }
            string url = args[0];

            Updater? updater = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                updater = new WindowsUpdater();
            }

            updater?.Update(url);
        }

    }
}