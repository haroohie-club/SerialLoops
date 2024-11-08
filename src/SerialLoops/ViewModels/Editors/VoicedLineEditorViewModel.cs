using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using static HaruhiChokuretsuLib.Archive.Event.VoiceMapFile;
using static SerialLoops.Lib.Script.Parameters.ScreenScriptParameter;

namespace SerialLoops.ViewModels.Editors;

public class VoicedLineEditorViewModel : EditorViewModel
{
    private VoicedLineItem _vce;
    private VoiceMapEntry _voiceMapEntry;
    private string _subtitle;

    public ICommand ReplaceCommand { get; set; }
    public ICommand ExportCommand { get; set; }
    public ICommand RestoreCommand { get; set; }

    [Reactive]
    public SoundPlayerPanelViewModel VcePlayer { get; set; }
    public ScreenSelectorViewModel ScreenSelector { get; set; }
    [Reactive]
    public SKBitmap SubtitlesPreview { get; set; } = new(256, 384);

    private DsScreen _subtitleScreen;
    public DsScreen SubtitleScreen
    {
        get => _subtitleScreen;
        set
        {
            this.RaiseAndSetIfChanged(ref _subtitleScreen, value);
            if (_voiceMapEntry is not null)
            {
                _voiceMapEntry.TargetScreen = SubtitleScreen == DsScreen.BOTTOM ? VoiceMapEntry.Screen.BOTTOM : VoiceMapEntry.Screen.TOP;
                UpdatePreview();
                Description.UnsavedChanges = true;
            }
        }
    }

    private VoiceMapEntry.YPosition _yPos;

    public bool TopY
    {
        get => _yPos == VoiceMapEntry.YPosition.TOP;
        set
        {
            this.RaiseAndSetIfChanged(ref _yPos, VoiceMapEntry.YPosition.TOP);
            if (_voiceMapEntry is not null)
            {
                _voiceMapEntry.YPos = _yPos;
                UpdatePreview();
                Description.UnsavedChanges = true;
            }
        }
    }
    public bool BelowTopY
    {
        get => _yPos == VoiceMapEntry.YPosition.BELOW_TOP;
        set
        {
            this.RaiseAndSetIfChanged(ref _yPos, VoiceMapEntry.YPosition.BELOW_TOP);
            if (_voiceMapEntry is not null)
            {
                _voiceMapEntry.YPos = _yPos;
                UpdatePreview();
                Description.UnsavedChanges = true;
            }
        }
    }
    public bool AboveBottomY
    {
        get => _yPos == VoiceMapEntry.YPosition.ABOVE_BOTTOM;
        set
        {
            this.RaiseAndSetIfChanged(ref _yPos, VoiceMapEntry.YPosition.ABOVE_BOTTOM);
            if (_voiceMapEntry is not null)
            {
                _voiceMapEntry.YPos = _yPos;
                UpdatePreview();
                Description.UnsavedChanges = true;
            }
        }
    }
    public bool BottomY
    {
        get => _yPos == VoiceMapEntry.YPosition.BOTTOM;
        set
        {
            this.RaiseAndSetIfChanged(ref _yPos, VoiceMapEntry.YPosition.BOTTOM);
            if (_voiceMapEntry is not null)
            {
                _voiceMapEntry.YPos = _yPos;
                UpdatePreview();
                Description.UnsavedChanges = true;
            }
        }
    }

    public string Subtitle
    {
        get => _project.LangCode.Equals("ja") ? _subtitle : (_subtitle?.GetSubstitutedString(_project) ?? string.Empty);
        set
        {
            this.RaiseAndSetIfChanged(ref _subtitle, _project.LangCode.Equals("ja") ? value : value.GetOriginalString(_project));
            if (_voiceMapEntry is null)
            {
                _project.VoiceMap.VoiceMapEntries.Add(new()
                {
                    VoiceFileName = Path.GetFileNameWithoutExtension(_vce.VoiceFile),
                    FontSize = 100,
                    TargetScreen = SubtitleScreen == DsScreen.BOTTOM ? VoiceMapEntry.Screen.BOTTOM : VoiceMapEntry.Screen.TOP,
                    Timer = 350,
                });
                _project.VoiceMap.VoiceMapEntries[^1].SetSubtitle(_subtitle, _project.FontReplacement);
                _project.VoiceMap.VoiceMapEntries[^1].YPos = _yPos;
                _voiceMapEntry = _project.VoiceMap.VoiceMapEntries[^1];
            }
            else
            {
                _voiceMapEntry.SetSubtitle(_subtitle, _project.FontReplacement);
            }
            UpdatePreview();
            Description.UnsavedChanges = true;
        }
    }

    public bool SubsEnabled => _window.OpenProject.VoiceMap is not null;

    public VoicedLineEditorViewModel(VoicedLineItem item, MainWindowViewModel window, ILogger log) : base(item, window, log, window.OpenProject)
    {
        _vce = item;
        VcePlayer = new(_vce, log, null);
        ReplaceCommand = ReactiveCommand.CreateFromTask(Replace);
        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        RestoreCommand = ReactiveCommand.Create(Restore);

        ScreenSelector = new(DsScreen.BOTTOM, false);
        ScreenSelector.ScreenChanged += (sender, args) =>
        {
            SubtitleScreen = ScreenSelector.SelectedScreen;
        };

        _voiceMapEntry = _project.VoiceMap.VoiceMapEntries.FirstOrDefault(v => v.VoiceFileName.Equals(Path.GetFileNameWithoutExtension(_vce.VoiceFile)));
        if (_voiceMapEntry is not null)
        {
            _subtitle = _voiceMapEntry.Subtitle;
            _subtitleScreen = _voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.BOTTOM ? DsScreen.BOTTOM : DsScreen.TOP;
            _yPos = _voiceMapEntry.YPos;
            UpdatePreview();
        }
    }

    private async Task Replace()
    {
        IStorageFile openFile = await _window.Window.ShowOpenFilePickerAsync(Strings.Replace_voiced_line, [new FilePickerFileType(Strings.Supported_Audio_Files) { Patterns = Shared.SupportedAudioFiletypes },
            new FilePickerFileType(Strings.WAV_files) { Patterns = ["*.wav"] }, new FilePickerFileType(Strings.FLAC_files) { Patterns = ["*.flac"] },
            new FilePickerFileType(Strings.MP3_files) { Patterns = ["*.mp3"] }, new FilePickerFileType(Strings.Vorbis_files) { Patterns = ["*.ogg"] }]);
        if (openFile is not null)
        {
            LoopyProgressTracker tracker = new();
            VcePlayer.Stop();
            await new ProgressDialog(() => _vce.Replace(openFile.Path.LocalPath, _project.BaseDirectory, _project.IterativeDirectory, Path.Combine(_project.Config.CachesDirectory, "vce", $"{_vce.Name}.wav"), _log),
                () => { }, tracker, Strings.Replace_voiced_line).ShowDialog(_window.Window);
        }
    }

    private async Task Export()
    {
        IStorageFile saveFile = await _window.Window.ShowSaveFilePickerAsync(Strings.Save_voiced_line_as_WAV, [new FilePickerFileType(Strings.WAV_File) { Patterns = ["*.wav"] }]);
        if (saveFile is not null)
        {
            WaveFileWriter.CreateWaveFile(saveFile.Path.LocalPath, _vce.GetWaveProvider(_log));
        }
    }

    private void Restore()
    {

    }

    private void UpdatePreview()
    {
        SubtitlesPreview = new(256, 384);
        SKCanvas canvas = new(SubtitlesPreview);
        canvas.DrawColor(SKColors.DarkGray);
        canvas.DrawLine(new SKPoint { X = 0, Y = 192 }, new SKPoint { X = 256, Y = 192 }, DialogueScriptParameter.Paint00);

        bool bottomScreen = _voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.BOTTOM;
        if (bottomScreen)
        {
            for (int i = 0; i <= 1; i++)
            {
                canvas.DrawHaroohieText(
                    _subtitle,
                    DialogueScriptParameter.Paint07,
                    _project,
                    i + _voiceMapEntry.X,
                    1 + _voiceMapEntry.Y + (bottomScreen ? 192 : 0),
                    false
                );
            }
        }

        canvas.DrawHaroohieText(
            _subtitle,
            DialogueScriptParameter.Paint00,
            _project,
            _voiceMapEntry.X,
            _voiceMapEntry.Y + (bottomScreen ? 192 : 0),
            false
        );

        canvas.Flush();
    }
}