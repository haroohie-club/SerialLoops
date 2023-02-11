using NAudio.Wave;
using System;

namespace SerialLoops.Lib.Util
{
    public class WaveProviderStream : WaveStream
    {
        private readonly IWaveProvider _source;
        private long position;

        public WaveProviderStream(IWaveProvider source)
        {
            _source = source;
        }

        public override WaveFormat WaveFormat => _source.WaveFormat;

        public override bool CanSeek => false;

        public override long Length => int.MaxValue;

        public override long Position { get => position; set => throw new NotImplementedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);
            position += read;
            return read;
        }
    }
}
