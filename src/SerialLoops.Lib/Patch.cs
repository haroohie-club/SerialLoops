using System.IO;
using VCDiff.Encoders;

namespace SerialLoops.Lib
{
    public static class Patch
    {
        public static void CreatePatch(string baseRom, string currentRom, string outputFile)
        {
            using FileStream baseRomStream = File.OpenRead(baseRom);
            using FileStream currentRomStream = File.OpenRead(currentRom);
            using FileStream outputFileStream = File.Create(outputFile);
            VcEncoder encoder = new(baseRomStream, currentRomStream, outputFileStream);
            encoder.Encode();
        }
    }
}
