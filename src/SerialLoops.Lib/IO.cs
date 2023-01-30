using SerialLoops.Lib.Logging;
using System.IO;

namespace SerialLoops.Lib
{
    public static class IO
    {
        public static void OpenRom(Project project, string romPath)
        {
            // NitroPacker unpack to base directory
            // NitroPacker unpack to iterative directory


        }

        public static bool WriteStringFile(string file, string str, ILogger log)
        {
            try
            {
                File.WriteAllText(file, str);
                return true;
            }
            catch (IOException exc)
            {
                log.LogError($"Exception occurred while writing file '{file}' to disk.\n{exc.Message}\n\n{exc.StackTrace}");
                return false;
            }
        }
    }
}
