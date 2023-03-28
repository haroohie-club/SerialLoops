using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using SerialLoops.Lib.Items;

namespace SerialLoops.Dialogs
{
    public class BgmGainPropertiesDialog : Dialog
    {

    }

    public class BgmVolumePreviewItem : ISoundItem
    {
        public ISampleProvider Provider { get; set; }

        public IWaveProvider GetWaveProvider(ILogger log, bool loop)
        {
            return Provider.ToWaveProvider16();
        }
    }
}
