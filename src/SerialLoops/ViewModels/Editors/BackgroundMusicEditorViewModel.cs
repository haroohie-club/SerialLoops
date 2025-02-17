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
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors;

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

    public BackgroundMusicEditorViewModel(BackgroundMusicItem bgm, MainWindowViewModel window, Project project, ILogger log, bool initializePlayer = true) : base(bgm, window, log, project)
    {
        Bgm = bgm;

        _bgmCachedFile = Path.Combine(project.Config.CachesDirectory, "bgm", $"{Bgm.Name}.wav");

        _titleBoxTextChangedCommand = ReactiveCommand.Create<string>(TitleBox_TextChanged);
        BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null, initializePlayer);
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
            Bgm.DisplayName = $"{Bgm.Name} - {Bgm.BgmName}";
            Bgm.UnsavedChanges = true;
        }
    }

    private async Task Extract_Executed()
    {
        IStorageFile file = await Window.Window.ShowSaveFilePickerAsync(Strings.Save_BGM_as_WAV, [new(Strings.WAV_File) { Patterns = ["*.wav"] }]);
        if (file is not null)
        {
            ProgressDialogViewModel tracker = new(Strings.Exporting_BGM);
            tracker.InitializeTasks(
                () => WaveFileWriter.CreateWaveFile(file.Path.LocalPath, Bgm.GetWaveProvider(_log, false)),
                () => { });
            await new ProgressDialog { DataContext = tracker }.ShowDialog(Window.Window);
        }
    }

    private async Task Replace_Executed()
    {
        IStorageFile file = await Window.Window.ShowOpenFilePickerAsync(Strings.Replace_BGM,
        [
            new(Strings.Supported_Audio_Files) { Patterns = Shared.SupportedAudioFiletypes },
            new(Strings.WAV_files) { Patterns = ["*.wav"] },
            new(Strings.FLAC_files) { Patterns = ["*.flac"] },
            new(Strings.MP3_files) { Patterns = ["*.mp3"] },
            new(Strings.Vorbis_files) { Patterns = ["*.ogg"] },
        ]);
        if (file is not null)
        {
            ProgressDialogViewModel firstTracker = new(Strings.Replace_BGM_track, Strings.Replacing_BGM);
            ProgressDialogViewModel secondTracker = new(Strings.Replace_BGM_track, Strings.Replacing_BGM);
            BgmPlayer.Stop();
            Shared.AudioReplacementCancellation.Cancel();
            Shared.AudioReplacementCancellation = new();
            firstTracker.InitializeTasks(() => Bgm.Replace(file.Path.LocalPath, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, secondTracker, Shared.AudioReplacementCancellation.Token),
            () => BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null));
            await new ProgressDialog { DataContext = firstTracker }.ShowDialog(Window.Window);
        }
    }

    private void Restore_Executed()
    {
        BgmPlayer.Stop();
        if (Directory.Exists(_project.Config.CachesDirectory))
        {
            File.Delete(_bgmCachedFile); // Clear the cached WAV as we're restoring the original ADX
        }
        File.Copy(Path.Combine(_project.BaseDirectory, "original", "bgm", Path.GetFileName(Bgm.BgmFile)), Path.Combine(_project.BaseDirectory, Bgm.BgmFile), true);
        File.Copy(Path.Combine(_project.IterativeDirectory, "original", "bgm", Path.GetFileName(Bgm.BgmFile)), Path.Combine(_project.IterativeDirectory, Bgm.BgmFile), true);
        BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null);
    }

    private async Task ManageLoop_Executed()
    {
        BgmPlayer.Stop();
        ProgressDialogViewModel firstTracker = new(Strings.Caching_BGM, Strings.Adjusting_Loop_Info);
        firstTracker.InitializeTasks(() => WaveFileWriter.CreateWaveFile(_bgmCachedFile, Bgm.GetWaveProvider(_log, false)), () => { });
        if (!File.Exists(_bgmCachedFile))
        {
            await new ProgressDialog { DataContext = firstTracker }.ShowDialog(Window.Window);
        }
        string loopAdjustedWav = Path.Combine(Path.GetDirectoryName(_bgmCachedFile), $"{Path.GetFileNameWithoutExtension(_bgmCachedFile)}-loop.wav");
        File.Copy(_bgmCachedFile, loopAdjustedWav, true);
        using WaveFileReader reader = new(loopAdjustedWav);
        BgmLoopPropertiesDialogViewModel loopPropertiesDialog = new(reader, Bgm.Name, _log,
            ((AdxWaveProvider)BgmPlayer.Sound).LoopEnabled, ((AdxWaveProvider)BgmPlayer.Sound).LoopStartSample, ((AdxWaveProvider)BgmPlayer.Sound).LoopEndSample);
        BgmLoopPreviewItem loopPreview = await new BgmLoopPropertiesDialog { DataContext = loopPropertiesDialog }.ShowDialog<BgmLoopPreviewItem>(Window.Window);
        if (loopPreview is not null)
        {
            _loopEnabled = loopPreview.LoopEnabled;
            _loopStartSample = loopPreview.StartSample;
            _loopEndSample = loopPreview.EndSample;
            Shared.AudioReplacementCancellation.Cancel();
            Shared.AudioReplacementCancellation = new();
            ProgressDialogViewModel secondTracker = new(Strings.Set_BGM_loop_info, Strings.Adjusting_Loop_Info);
            ProgressDialogViewModel thirdTracker = new(Strings.Set_BGM_loop_info, Strings.Adjusting_Loop_Info);
            thirdTracker.InitializeTasks(() =>
                {
                    Bgm.Replace(_bgmCachedFile, _project.BaseDirectory, _project.IterativeDirectory, _bgmCachedFile, _loopEnabled, _loopStartSample, _loopEndSample, _log, secondTracker, Shared.AudioReplacementCancellation.Token);
                    reader.Dispose();
                    File.Delete(loopAdjustedWav);
                },
                () =>
                {
                    BgmPlayer = new(Bgm, _log, Bgm.BgmName, Bgm.Name, Bgm.Flag, !string.IsNullOrEmpty(Bgm.BgmName) ? _titleBoxTextChangedCommand : null);
                });
            await new ProgressDialog { DataContext = thirdTracker }.ShowDialog(Window.Window);
        }
    }

    private async Task AdjustVolume_Executed()
    {
        BgmPlayer.Stop();
        ProgressDialogViewModel firstTracker = new(Strings.Caching_BGM, Strings.Adjusting_Volume);
        firstTracker.InitializeTasks(() => WaveFileWriter.CreateWaveFile(_bgmCachedFile, Bgm.GetWaveProvider(_log, false)), () => { });
        if (!File.Exists(_bgmCachedFile))
        {
            await new ProgressDialog { DataContext = firstTracker }.ShowDialog(Window.Window);
        }
        string volumeAdjustedWav = Path.Combine(Path.GetDirectoryName(_bgmCachedFile), $"{Path.GetFileNameWithoutExtension(_bgmCachedFile)}-volume.wav");
        File.Copy(_bgmCachedFile, volumeAdjustedWav, true);
        await using WaveFileReader reader = new(volumeAdjustedWav);
        BgmVolumePropertiesDialogViewModel volumeDialog = new(reader, Bgm.Name, _project.AverageBgmMaxAmplitude, _log);
        BgmVolumePreviewItem volumePreview = await new BgmVolumePropertiesDialog { DataContext = volumeDialog }.ShowDialog<BgmVolumePreviewItem>(Window.Window);
        if (volumePreview is not null)
        {
            ProgressDialogViewModel secondTracker = new(Strings.Replace_BGM_track, Strings.Adjusting_Volume) { Total = 2 };
            ProgressDialogViewModel thirdTracker = new(Strings.Replace_BGM_track, Strings.Adjusting_Volume);
            BgmPlayer.Stop();
            Shared.AudioReplacementCancellation.Cancel();
            Shared.AudioReplacementCancellation = new();
            thirdTracker.InitializeTasks(() =>
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
            });
            await new ProgressDialog { DataContext = thirdTracker }.ShowDialog(Window.Window);
        }
    }
}
