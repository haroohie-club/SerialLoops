using System;
using System.IO;
using HaruhiChokuretsuLib.Util;
using VCDiff.Encoders;

namespace SerialLoops.Lib
{
    public static class Patch
    {
        public static void CreatePatch(string baseRom, string currentRom, string outputFile, ILogger log)
        {
            try
            {
                using FileStream baseRomStream = File.OpenRead(baseRom);
                using FileStream currentRomStream = File.OpenRead(currentRom);
                using FileStream outputFileStream = File.Create(outputFile);
                VcEncoder encoder = new(baseRomStream, currentRomStream, outputFileStream);
                encoder.Encode();
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to create patch of base ROM '{baseRom}' from current ROM '{currentRom}' outputting to '{outputFile}'", ex);
            }
        }
    }
}
