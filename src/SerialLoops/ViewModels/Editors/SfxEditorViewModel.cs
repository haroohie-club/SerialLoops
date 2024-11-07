using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using GotaSequenceLib.Playback;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;

namespace SerialLoops.ViewModels.Editors
{
    public class SfxEditorViewModel : EditorViewModel
    {
        [Reactive]
        public SfxItem Sfx { get; set; }
        private SequenceArchive _archive;
        private SequenceArchiveSequence _sequence;
        private Player _player;

        [Reactive]
        public SfxPlayerPanelViewModel SfxPlayerPanel { get; set; }
        public string Groups => string.Format(Strings.Groups___0_, string.Join(", ", Sfx.AssociatedGroups));

        public ICommand ExtractCommand { get; }

        public SfxEditorViewModel(ItemDescription item, MainWindowViewModel window, ILogger log) : base(item, window, log, window.OpenProject)
        {
            Sfx = (SfxItem)Description;
            _archive = _project.Snd.SequenceArchives[Sfx.Entry.SequenceArchive].File;
            _sequence = _archive.Sequences[Sfx.Entry.Index];

            _player = new(new(new SfxMixer().Player));
            _player.PrepareForSong([_sequence.Bank.File], _sequence.Bank.GetAssociatedWaves());
            _archive.ReadCommandData();
            _player.LoadSong(_archive.Commands, _archive.PublicLabels.ElementAt(_archive.Sequences.IndexOf(_sequence)).Value);

            SfxPlayerPanel = new(_player, _log);
            ExtractCommand = ReactiveCommand.CreateFromTask(Extract);
        }

        private async Task Extract()
        {
            _player.Stop();
            IStorageFile wavFile = await GuiExtensions.ShowSaveFilePickerAsync(_window.Window, Strings.Export_SFX, [new(Strings.WAV_File) { Patterns = ["*.wav"] }], $"{Sfx.DisplayName}.wav");
            if (wavFile is not null)
            {
                _player.Record(wavFile.TryGetLocalPath());
            }
        }
    }
}
