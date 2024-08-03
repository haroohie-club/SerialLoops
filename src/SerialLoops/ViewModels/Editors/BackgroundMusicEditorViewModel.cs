using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Audio.ADX;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors
{
    public class BackgroundMusicEditorViewModel : EditorViewModel
    {
        public BackgroundMusicItem Bgm { get; set; }
        [Reactive]
        public SoundPlayerPanelViewModel BgmPlayer { get; set; }
        private bool _loopEnabled;
        private uint _loopStartSample;
        private uint _loopEndSample;

        private readonly string _bgmCachedFile;

        public ICommand ManageLoopCommand { get; set; }
        public ICommand AdjustVolumeCommand { get; set; }
        public ICommand ExtractCommand { get; set; }
        public ICommand ReplaceCommand { get; set; }
        public ICommand RestoreCommand { get; set; }

        private ICommand _titleBoxTextChangedCommand;

        public BackgroundMusicEditorViewModel(BackgroundMusicItem bgm, MainWindowViewModel window, Project project, ILogger log) : base(bgm, window, log, project)
        {
            Bgm = bgm;

            _bgmCachedFile = Path.Combine(project.Config.CachesDirectory, "bgm", $"{Bgm.Name}.wav");

            _titleBoxTextChangedCommand = ReactiveCommand.Create<string>(TitleBox_TextChanged);
            BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null);
            ManageLoopCommand = ReactiveCommand.CreateFromTask(ManageLoop_Executed);
            AdjustVolumeCommand = ReactiveCommand.CreateFromTask(AdjustVolume_Executed);
            ExtractCommand = ReactiveCommand.CreateFromTask(Extract_Executed);
            ReplaceCommand = ReactiveCommand.CreateFromTask(Replace_Executed);
            RestoreCommand = ReactiveCommand.Create(Restore_Executed);
        }

        private void TitleBox_TextChanged(string newText)
        {
            if (!string.IsNullOrEmpty(newText) && !newText.Equals(Bgm.BgmName))
            {
                _project.Extra.Bgms[_project.Extra.Bgms.IndexOf(_project.Extra.Bgms.First(b => b.Name.GetSubstitutedString(_project) == Bgm.BgmName))].Name = newText.GetOriginalString(_project);
                Bgm.BgmName = newText;
                Bgm.UnsavedChanges = true;
            }
        }

        private async Task Extract_Executed()
        {
            IStorageFile file = await _window.Window.ShowSaveFilePickerAsync(Strings.Save_BGM_as_WAV, [new(Strings.WAV_File) { Patterns = ["*.wav"] }]);
            if (file is not null)
            {
                LoopyProgressTracker tracker = new();
                await new ProgressDialog(() => WaveFileWriter.CreateWaveFile(file.Path.LocalPath, Bgm.GetWaveProvider(_log, false)),
                    () => { }, tracker, Strings.Exporting_BGM).ShowDialog(_window.Window);
            }
        }

        private async Task Replace_Executed()
        {
            IStorageFile file = await _window.Window.ShowOpenFilePickerAsync(Strings.Replace_BGM,
                [
                    new(Strings.Supported_Audio_Files) { Patterns = Shared.SupportedAudioFiletypes },
                    new(Strings.WAV_files) { Patterns = ["*.wav"] },
                    new(Strings.FLAC_files) { Patterns = ["*.flac"] },
                    new(Strings.MP3_files) { Patterns = ["*.mp3"] },
                    new(Strings.Vorbis_files) { Patterns = ["*.ogg"] },
                ]);
            if (file is not null)
            {
                LoopyProgressTracker firstTracker = new(Strings.Replacing_BGM);
                LoopyProgressTracker secondTracker = new(Strings.Replacing_BGM);
                BgmPlayer.Stop();
                Shared.AudioReplacementCancellation.Cancel();
                Shared.AudioReplacementCancellation = new();
                await new ProgressDialog(() => Bgm.Replace(file.Path.LocalPath, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, secondTracker, Shared.AudioReplacementCancellation.Token),
                    () => BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null), firstTracker, Strings.Replace_BGM_track).ShowDialog(_window.Window);
            }
        }

        private void Restore_Executed()
        {
            BgmPlayer.Stop();
            File.Delete(_bgmCachedFile); // Clear the cached WAV as we're restoring the original ADX
            File.Copy(Path.Combine(_project.BaseDirectory, "original", "bgm", Path.GetFileName(Bgm.BgmFile)), Path.Combine(_project.BaseDirectory, Bgm.BgmFile), true);
            File.Copy(Path.Combine(_project.IterativeDirectory, "original", "bgm", Path.GetFileName(Bgm.BgmFile)), Path.Combine(_project.IterativeDirectory, Bgm.BgmFile), true);
            BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null);
        }

        private async Task ManageLoop_Executed()
        {
            BgmPlayer.Stop();
            LoopyProgressTracker firstTracker = new(Strings.Adjusting_Loop_Info);
            if (!File.Exists(_bgmCachedFile))
            {
                await new ProgressDialog(() => WaveFileWriter.CreateWaveFile(_bgmCachedFile, Bgm.GetWaveProvider(_log, false)), () => { }, firstTracker, Strings.Caching_BGM).ShowDialog(_window.Window);
            }
            string loopAdjustedWav = Path.Combine(Path.GetDirectoryName(_bgmCachedFile), $"{Path.GetFileNameWithoutExtension(_bgmCachedFile)}-loop.wav");
            File.Copy(_bgmCachedFile, loopAdjustedWav, true);
            using WaveFileReader reader = new(loopAdjustedWav);
            BgmLoopPropertiesDialogViewModel loopPropertiesDialog = new(reader, Bgm.Name, _log,
                ((AdxWaveProvider)BgmPlayer.Sound).LoopEnabled, ((AdxWaveProvider)BgmPlayer.Sound).LoopStartSample, ((AdxWaveProvider)BgmPlayer.Sound).LoopEndSample);
            BgmLoopPreviewItem loopPreview = await new BgmLoopPropertiesDialog() { DataContext = loopPropertiesDialog }.ShowDialog<BgmLoopPreviewItem>(_window.Window);
            if (loopPreview is not null)
            {
                _loopEnabled = loopPreview.LoopEnabled;
                _loopStartSample = loopPreview.StartSample;
                _loopEndSample = loopPreview.EndSample;
                Shared.AudioReplacementCancellation.Cancel();
                Shared.AudioReplacementCancellation = new();
                LoopyProgressTracker secondTracker = new(Strings.Adjusting_Loop_Info);
                LoopyProgressTracker thirdTracker = new(Strings.Adjusting_Loop_Info);
                await new ProgressDialog(() =>
                {
                    Bgm.Replace(_bgmCachedFile, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, secondTracker, Shared.AudioReplacementCancellation.Token);
                    reader.Dispose();
                    File.Delete(loopAdjustedWav);
                },
                () =>
                {
                    BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null);
                }, thirdTracker, Strings.Set_BGM_loop_info).ShowDialog(_window.Window);
            }
        }

        private async Task AdjustVolume_Executed()
        {
            BgmPlayer.Stop();
            LoopyProgressTracker firstTracker = new(Strings.Adjusting_Volume);
            if (!File.Exists(_bgmCachedFile))
            {
                await new ProgressDialog(() => WaveFileWriter.CreateWaveFile(_bgmCachedFile, Bgm.GetWaveProvider(_log, false)), () => { }, firstTracker, Strings.Caching_BGM).ShowDialog(_window.Window);
            }
            string volumeAdjustedWav = Path.Combine(Path.GetDirectoryName(_bgmCachedFile), $"{Path.GetFileNameWithoutExtension(_bgmCachedFile)}-volume.wav");
            File.Copy(_bgmCachedFile, volumeAdjustedWav, true);
            using WaveFileReader reader = new(volumeAdjustedWav);
            BgmVolumePropertiesDialogViewModel volumeDialog = new(reader, Bgm.Name, _log);
            BgmVolumePreviewItem volumePreview = await new BgmVolumePropertiesDialog() { DataContext = volumeDialog }.ShowDialog<BgmVolumePreviewItem>(_window.Window);
            if (volumePreview is not null)
            {
                LoopyProgressTracker secondTracker = new(Strings.Adjusting_Volume) { Total = 2 };
                LoopyProgressTracker thirdTracker = new(Strings.Adjusting_Volume);
                BgmPlayer.Stop();
                Shared.AudioReplacementCancellation.Cancel();
                Shared.AudioReplacementCancellation = new();
                await new ProgressDialog(() =>
                {
                    WaveFileWriter.CreateWaveFile(_bgmCachedFile, volumeDialog.VolumePreview.GetWaveProvider(_log, false));
                    secondTracker.Finished++;
                    Bgm.Replace(_bgmCachedFile, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, secondTracker, Shared.AudioReplacementCancellation.Token);
                    secondTracker.Finished++;
                    reader.Dispose();
                    File.Delete(volumeAdjustedWav);
                }, () =>
                {
                    BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null);
                }, thirdTracker, Strings.Replace_BGM_track).ShowDialog(_window.Window);
            }
        }
    }
}
