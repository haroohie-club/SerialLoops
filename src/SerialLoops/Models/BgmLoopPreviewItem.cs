using System;
using System.IO;
using System.Threading.Tasks;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SerialLoops.Lib.Items;

namespace SerialLoops.Models;

public partial class BgmLoopPreviewItem(WaveStream wav, bool loopEnabled, uint startSample, uint endSample) : ReactiveObject, ISoundItem, IDisposable, IAsyncDisposable
{
    public WaveStream Wave { get; } = wav;
    [Reactive]
    public partial bool LoopEnabled { get; set; } = loopEnabled;
    [Reactive]
    public partial uint StartSample { get; set; } = startSample;
    [Reactive]
    public partial uint EndSample { get; set; } = endSample;

    public IWaveProvider GetWaveProvider(ILogger log, bool loop)
    {
        MemoryStream stream = new();
        RawSourceWaveStream loopStream = new(stream, Wave.WaveFormat);
        byte[] fiveSecondBuffer = new byte[5 * (Wave.WaveFormat.BitsPerSample / 4) * Wave.WaveFormat.SampleRate];

        long startLoc = StartSample * Wave.WaveFormat.BitsPerSample / 8 * Wave.WaveFormat.Channels;
        long endLoc = EndSample * (Wave.WaveFormat.BitsPerSample / 8 * Wave.WaveFormat.Channels);

        Wave.Seek(Math.Max(startLoc, endLoc - fiveSecondBuffer.Length), SeekOrigin.Begin);
        Wave.ReadExactly(fiveSecondBuffer);
        stream.Write(fiveSecondBuffer);

        Wave.Seek(startLoc, SeekOrigin.Begin);
        Wave.ReadExactly(fiveSecondBuffer, 0, (int)Math.Min(Wave.Length - startLoc, fiveSecondBuffer.Length));
        stream.Write(fiveSecondBuffer);

        loopStream.Seek(0, SeekOrigin.Begin);
        return loopStream.ToSampleProvider().ToWaveProvider16();
    }

    public double GetTimestampFromSample(uint sample)
    {
        return (double)sample / Wave.WaveFormat.SampleRate;
    }
    public uint GetSampleFromTimestamp(double timestamp)
    {
        return (uint)(timestamp * Wave.WaveFormat.SampleRate);
    }

    public void Dispose()
    {
        Wave?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (Wave != null)
        {
            await Wave.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
