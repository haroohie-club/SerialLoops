using Eto.Forms;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using System;
using System.IO;

namespace SerialLoops.Editors
{
    public class BackgroundMusicEditor : Editor
    {
        private BackgroundMusicItem _bgm;
        public SoundPlayerPanel BgmPlayer { get; set; }

        public BackgroundMusicEditor(BackgroundMusicItem item, ILogger log, IProgressTracker tracker) : base(item, log, tracker)
        {
        }

        public override Container GetEditorPanel()
        {
            _bgm = (BackgroundMusicItem)Description;
            
            _tracker.Focus("BGM ADX data", 1);
            byte[] adxBytes = Array.Empty<byte>();
            try
            {
                adxBytes = File.ReadAllBytes(_bgm.BgmFile);
            }
            catch
            {
                if (!File.Exists(_bgm.BgmFile))
                {
                    _log.LogError($"Failed to load BGM file {_bgm.BgmFile}: file not found.");
                }
                else
                {
                    _log.LogError($"Failed to load BGM file {_bgm.BgmFile}: file invalid.");
                }
            }
            _tracker.Loaded++;

            AdxWaveProvider waveProvider = new(new AdxDecoder(adxBytes, _log));
            BgmPlayer = new(waveProvider, _log, _tracker);
            
            return new TableLayout(new TableRow(new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    _bgm.Name,
                    _bgm.BgmName,
                }
            }),
            new TableRow(BgmPlayer));
        }
    }
}
