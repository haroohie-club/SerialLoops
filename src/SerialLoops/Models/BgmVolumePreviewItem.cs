using System.IO;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;

namespace SerialLoops.Models
{
    public class BgmVolumePreviewItem : ReactiveObject, ISoundItem
    {
        private WaveStream _wav;
        [Reactive]
        public VolumeSampleProvider Provider { get; set; }

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
