using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

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

            return new TableLayout(
                new TableRow(ControlGenerator.GetPlayerStackLayout(VcePlayer, _vce.Name, _vce.AdxType.ToString())),
                new TableRow() // todo extract / replace buttons
                );
        }
    }
}
