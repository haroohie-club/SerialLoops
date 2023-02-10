using Eto.Forms;
using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using System;
using System.IO;

namespace SerialLoops.Editors
{
    public class BackgroundMusicEditor : Editor
    {
        private BackgroundMusicItem _bgm;

        public BackgroundMusicEditor(BackgroundMusicItem item, ILogger log) : base(item, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _bgm = (BackgroundMusicItem)Description;

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

            AdxWaveProvider waveProvider = new(new AdxDecoder(adxBytes, _log));
            SoundPlayerPanel bgmPlayer = new(waveProvider, _log);
            
            return new TableLayout(new TableRow(new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    Path.GetFileNameWithoutExtension(_bgm.BgmFile),
                    _bgm.BgmName,
                }
            },
            new TableRow(bgmPlayer)));
        }
    }
}
