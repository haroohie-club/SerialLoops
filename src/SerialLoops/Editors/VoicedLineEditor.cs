using Eto.Forms;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using System;
using System.IO;

namespace SerialLoops.Editors
{
    public class VoicedLineEditor : Editor
    {
        private VoicedLineItem _vce;
        public SoundPlayerPanel VcePlayer { get; set; }

        public VoicedLineEditor(VoicedLineItem vce, ILogger log) : base(vce, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _vce = (VoicedLineItem)Description;

            byte[] adxBytes = Array.Empty<byte>();
            try
            {
                adxBytes = File.ReadAllBytes(_vce.VoiceFile);
            }
            catch
            {
                if (!File.Exists(_vce.VoiceFile))
                {
                    _log.LogError($"Failed to load voice file {_vce.VoiceFile}: file not found.");
                }
                else
                {
                    _log.LogError($"Failed to load voice file {_vce.VoiceFile}: file invalid.");
                }
            }

            _vce.AdxType = (AdxEncoding)adxBytes[4];

            IAdxDecoder decoder;
            if (_vce.AdxType == AdxEncoding.Ahx10 || _vce.AdxType == AdxEncoding.Ahx11)
            {
                decoder = new AhxDecoder(adxBytes, _log);
            }
            else
            {
                decoder = new AdxDecoder(adxBytes, _log);
            }

            AdxWaveProvider waveProvider = new(decoder);
            VcePlayer = new(waveProvider, _log);

            return new TableLayout(new TableRow(new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    _vce.Name,
                    _vce.AdxType.ToString(),
                }
            }),
            new TableRow(VcePlayer));
        }
    }
}
