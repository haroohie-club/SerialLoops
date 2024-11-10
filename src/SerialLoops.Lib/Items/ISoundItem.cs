using HaruhiChokuretsuLib.Util;
using NAudio.Wave;

namespace SerialLoops.Lib.Items;

public interface ISoundItem
{
    IWaveProvider GetWaveProvider(ILogger log, bool loop);
}

public static class SoundItem
{
    public const int MAX_SAMPLERATE = 32768;
}