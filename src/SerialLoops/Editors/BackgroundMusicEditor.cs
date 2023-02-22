using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class BackgroundMusicEditor : Editor
    {
        private BackgroundMusicItem _bgm;
        public SoundPlayerPanel BgmPlayer { get; set; }

        public BackgroundMusicEditor(BackgroundMusicItem item, ILogger log) : base(item, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _bgm = (BackgroundMusicItem)Description;
            BgmPlayer = new(_bgm, _log);

            return new TableLayout(
                new TableRow(ControlGenerator.GetPlayerStackLayout(BgmPlayer, _bgm.BgmName, _bgm.Name)),
                new TableRow() // todo extract / replace buttons
                );
        }
    }
}
