using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Lib.Items;

namespace SerialLoops.Models
{
    public class BgmLoopPreviewItem(WaveStream wav, bool loopEnabled, uint startSample, uint endSample) : ObservableObject, ISoundItem
    {
        private WaveStream _wave = wav;
        private bool _loopEnanbled = loopEnabled;
        private uint _startSample = startSample;
        private uint _endSample = endSample;

        public WaveStream Wave
        {
            get => _wave;
            set => SetProperty(ref _wave, value);
        }
        public bool LoopEnabled
        {
            get => _loopEnanbled;
            set => SetProperty(ref _loopEnanbled, value);
        }
        public uint StartSample
        {
            get => _startSample;
            set => SetProperty(ref _startSample, value);
        }
        public uint EndSample
        {
            get => _endSample;
            set => SetProperty(ref _endSample, value);
        }

        public IWaveProvider GetWaveProvider(ILogger log, bool loop)
        {
            MemoryStream stream = new();
            RawSourceWaveStream loopStream = new(stream, Wave.WaveFormat);
            byte[] fiveSecondBuffer = new byte[5 * (Wave.WaveFormat.BitsPerSample / 4) * Wave.WaveFormat.SampleRate];

            long startLoc = StartSample * Wave.WaveFormat.BitsPerSample / 8 * Wave.WaveFormat.Channels;
            long endLoc = EndSample * (Wave.WaveFormat.BitsPerSample / 8 * Wave.WaveFormat.Channels);

            Wave.Seek(Math.Max(startLoc, endLoc - fiveSecondBuffer.Length), SeekOrigin.Begin);
            Wave.Read(fiveSecondBuffer, 0, fiveSecondBuffer.Length);
            stream.Write(fiveSecondBuffer);

            Wave.Seek(startLoc, SeekOrigin.Begin);
            Wave.Read(fiveSecondBuffer, 0, (int)Math.Min(Wave.Length - startLoc, fiveSecondBuffer.Length));
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
    }
}
