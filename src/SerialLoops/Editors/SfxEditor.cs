using Eto.Forms;
using GotaSequenceLib.Playback;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System.Linq;

namespace SerialLoops.Editors
{
    public class SfxEditor : Editor
    {
        private SfxItem _sfx;
        private SequenceArchive _archive;
        private SequenceArchiveSequence _sequence;
        public Player Player { get; private set; }

        public SfxPlayerPanel SfxPlayer { get; set; }

        public SfxEditor(SfxItem sfx, Project project, ILogger log) : base(sfx, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _sfx = (SfxItem)Description;
            _archive = _project.Snd.SequenceArchives[_sfx.Entry.SequenceArchive].File;
            _sequence = _archive.Sequences[_sfx.Entry.Index];

            Player = new(new(new SfxMixer().WavePlayer));
            Player.PrepareForSong(new IPlayableBank[] { _sequence.Bank.File }, _sequence.Bank.GetAssociatedWaves());
            _archive.ReadCommandData();
            Player.LoadSong(_archive.Commands, _archive.PublicLabels.ElementAt(_archive.Sequences.IndexOf(_sequence)).Value);

            SfxPlayer = new(Player, _log);

            return new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    SfxPlayer,
                    _sfx.Name,
                }
            };
        }
    }
}
