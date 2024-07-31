using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Audio.ADX;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
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
        private SoundPlayerPanelViewModel _soundPlayerViewModel;

        public BackgroundMusicItem Bgm { get; set; }
        public SoundPlayerPanelViewModel SoundPlayerViewModel
        {
            get => _soundPlayerViewModel;
            set => SetProperty(ref _soundPlayerViewModel, value);
        }
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
            SoundPlayerViewModel = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null);
            ManageLoopCommand = ReactiveCommand.CreateFromTask(ManageLoop_Executed);
            AdjustVolumeCommand = ReactiveCommand.CreateFromTask(AdjustVolume_Executed);
        }

        private async Task ManageLoop_Executed()
        {
            SoundPlayerViewModel.Stop();
            LoopyProgressTracker tracker = new(Strings.Adjusting_Loop_Info);
            if (!File.Exists(_bgmCachedFile))
            {
                await new ProgressDialog(() => WaveFileWriter.CreateWaveFile(_bgmCachedFile, Bgm.GetWaveProvider(_log, false)), () => { }, tracker, Strings.Caching_BGM).ShowDialog(_window.Window);
            }
            string loopAdjustedWav = Path.Combine(Path.GetDirectoryName(_bgmCachedFile), $"{Path.GetFileNameWithoutExtension(_bgmCachedFile)}-loop.wav");
            File.Copy(_bgmCachedFile, loopAdjustedWav, true);
            using WaveFileReader reader = new(loopAdjustedWav);
            BgmLoopPropertiesDialogViewModel loopPropertiesDialog = new(reader, Bgm.Name, _log,
                ((AdxWaveProvider)SoundPlayerViewModel.Sound).LoopEnabled, ((AdxWaveProvider)SoundPlayerViewModel.Sound).LoopStartSample, ((AdxWaveProvider)SoundPlayerViewModel.Sound).LoopEndSample);
            BgmLoopPreviewItem loopPreview = await new BgmLoopPropertiesDialog() { DataContext = loopPropertiesDialog }.ShowDialog<BgmLoopPreviewItem>(_window.Window);
            if (loopPreview is not null)
            {
                _loopEnabled = loopPreview.LoopEnabled;
                _loopStartSample = loopPreview.StartSample;
                _loopEndSample = loopPreview.EndSample;
                Shared.AudioReplacementCancellation.Cancel();
                Shared.AudioReplacementCancellation = new();
                await new ProgressDialog(() =>
                {
                    Bgm.Replace(_bgmCachedFile, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, tracker, Shared.AudioReplacementCancellation.Token);
                    reader.Dispose();
                    File.Delete(loopAdjustedWav);
                },
                () =>
                {
                    SoundPlayerViewModel = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null);
                }, tracker, Strings.Set_BGM_loop_info).ShowDialog(_window.Window);
            }
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

        private async Task AdjustVolume_Executed()
        {

        }
    }
}
