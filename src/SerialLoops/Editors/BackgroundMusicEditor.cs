using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;

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
            
            return new TableLayout(new TableRow(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Items =
                    {
                        BgmPlayer,
                        new StackLayout
                        {
                            Orientation = Orientation.Vertical,
                            Spacing = 5,
                            Items = { _bgm.Name, _bgm.BgmName }
                        }
                    }
                }),
            new TableRow()); // todo extract / replace buttons
        }
    }
}
