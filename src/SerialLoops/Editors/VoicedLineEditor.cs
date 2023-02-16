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

            VcePlayer = new(_vce.GetAdxWaveProvider(_log), _log);

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
