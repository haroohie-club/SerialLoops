using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SerialLoops.Lib.Items;

namespace SerialLoops.Models
{
    public class BgmVolumePreviewItem : ObservableObject, ISoundItem
    {
        private WaveStream _wav;
        private VolumeSampleProvider _provider;
        public VolumeSampleProvider Provider
        {
            get => _provider;
            set => SetProperty(ref _provider, value);
        }

        public BgmVolumePreviewItem(WaveStream wav)
        {
            _wav = wav;
            _wav.Seek(0, SeekOrigin.Begin);
            Provider = new(wav.ToSampleProvider());
        }

        public void SetVolume(double volume)
        {
            Provider.Volume = (float)volume / 100f;
        }

        public IWaveProvider GetWaveProvider(ILogger log, bool loop)
        {
            _wav.Seek(0, SeekOrigin.Begin);
            return Provider.ToWaveProvider16();
        }
    }
}
