using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Concentus;
using Concentus.Oggfile;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;

namespace SerialLoops.Lib.Util;

public class OggOpusFileReader : IWaveProvider, IDisposable, IAsyncDisposable
{
    private FileStream _oggFileStream;
    private readonly OpusOggReadStream _opusOggReadStream;

    public OggOpusFileReader(string oggFile, ILogger log)
    {
        _oggFileStream = File.OpenRead(oggFile);
        _opusOggReadStream = new(OpusCodecFactory.CreateDecoder(48000, 2, new ChokuLogTextWriter(log)), _oggFileStream);
    }

    public int Read(Span<byte> buffer)
    {
        int i = 0;
        while (i < buffer.Length)
        {
            if (!_opusOggReadStream.HasNextPacket)
            {
                return i;
            }

            short[] nextSample = _opusOggReadStream.DecodeNextPacket();
            if (nextSample is null)
            {
                return i;
            }
            byte[] bytes = nextSample.SelectMany(BitConverter.GetBytes).ToArray();
            bytes.CopyTo(buffer[i..]);
            i += bytes.Length;
        }
        return i;
    }

    public WaveFormat WaveFormat => new(48000, 2);

    public void Dispose()
    {
        _oggFileStream?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_oggFileStream != null)
        {
            await _oggFileStream.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
