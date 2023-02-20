using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;

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

            VcePlayer = new(_vce, _log);

            return new TableLayout(new TableRow(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Items =
                    {
                        VcePlayer,
                        new StackLayout
                        {
                            Orientation = Orientation.Vertical,
                            Spacing = 5,
                            Items = { _vce.Name, _vce.AdxType.ToString() }
                        }
                    }
                }),
                new TableRow()); // todo extract / replace buttons
        }
    }
}
