using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio.ADX;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
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

    public ObservableCollection<LocalizedDialogueColor> SubtitleColors { get; } = new(Enum.GetValues<DialogueColor>().Select(c => new LocalizedDialogueColor(c)));
    private LocalizedDialogueColor _subtitleColor;
    public LocalizedDialogueColor SubtitleColor
    {
        get => _subtitleColor;
        set
        {
            this.RaiseAndSetIfChanged(ref _subtitleColor, value);
            if (_voiceMapEntry is not null)
            {
                _voiceMapEntry.Color = _subtitleColor.Color;
                UpdatePreview();
                Description.UnsavedChanges = true;
            }
        }
    }

    private DsScreen _subtitleScreen;
    public DsScreen SubtitleScreen
    {
        get => _subtitleScreen;
        set
        {
            this.RaiseAndSetIfChanged(ref _subtitleScreen, value);
            if (_voiceMapEntry is not null)
            {
                _voiceMapEntry.TargetScreen = SubtitleScreen == DsScreen.BOTTOM ? VoiceMapEntry.Screen.BOTTOM : _forceDropShadow ? VoiceMapEntry.Screen.TOP_FORCE_SHADOW : VoiceMapEntry.Screen.TOP;
                UpdatePreview();
                Description.UnsavedChanges = true;
            }
        }
    }

    private bool _forceDropShadow;
    public bool ForceDropShadow
    {
        get => _forceDropShadow;
        set
        {
            this.RaiseAndSetIfChanged(ref _forceDropShadow, value);
            if (_voiceMapEntry is not null)
            {
                if ((_voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.TOP ||
                    _voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.TOP_FORCE_SHADOW) && _forceDropShadow)
                {
                    _voiceMapEntry.TargetScreen = VoiceMapEntry.Screen.TOP_FORCE_SHADOW;
                    UpdatePreview();
                    Description.UnsavedChanges = true;
                }
                else if (_voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.TOP ||
                         _voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.TOP_FORCE_SHADOW)
                {
                    _voiceMapEntry.TargetScreen = VoiceMapEntry.Screen.TOP;
                    UpdatePreview();
                    Description.UnsavedChanges = true;
                }
            }
        }
    }

    private VoiceMapEntry.YPosition _yPos;

    public bool TopY
    {
        get => _yPos == VoiceMapEntry.YPosition.TOP;
        set
        {
            if (_voiceMapEntry is not null && value)
            {
                _yPos = VoiceMapEntry.YPosition.TOP;
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
            if (_voiceMapEntry is not null && value)
            {
                _yPos = VoiceMapEntry.YPosition.BELOW_TOP;
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
            if (_voiceMapEntry is not null && value)
            {
                _yPos = VoiceMapEntry.YPosition.ABOVE_BOTTOM;
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
            if (_voiceMapEntry is not null && value)
            {
                _yPos = VoiceMapEntry.YPosition.BOTTOM;
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
                AdxHeader header = new(File.ReadAllBytes(Path.Combine(_project.IterativeDirectory, _vce.VoiceFile)), _log);
                _project.VoiceMap.VoiceMapEntries.Add(new()
                {
                    VoiceFileName = Path.GetFileNameWithoutExtension(_vce.VoiceFile),
                    Color = DialogueColor.WHITE,
                    TargetScreen = SubtitleScreen == DsScreen.BOTTOM ? VoiceMapEntry.Screen.BOTTOM : VoiceMapEntry.Screen.TOP,
                    Timer = (int)((double)header.TotalSamples / header.SampleRate * 180 + 30),
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

    public bool SubsEnabled => Window.OpenProject.VoiceMap is not null;

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
            _subtitleColor = (int)_voiceMapEntry.Color == 100 ? SubtitleColors.First(c => c.Color == DialogueColor.WHITE) :
                SubtitleColors.First(c => c.Color == _voiceMapEntry.Color);
            _subtitle = _voiceMapEntry.Subtitle;
            _subtitleScreen = _voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.BOTTOM ? DsScreen.BOTTOM : DsScreen.TOP;
            _forceDropShadow = _voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.TOP_FORCE_SHADOW;
            _yPos = _voiceMapEntry.YPos;
            UpdatePreview();
        }
    }

    private async Task Replace()
    {
        IStorageFile openFile = await Window.Window.ShowOpenFilePickerAsync(Strings.Replace_voiced_line, [new(Strings.Supported_Audio_Files) { Patterns = Shared.SupportedAudioFiletypes },
            new(Strings.WAV_files) { Patterns = ["*.wav"] }, new(Strings.FLAC_files) { Patterns = ["*.flac"] },
            new(Strings.MP3_files) { Patterns = ["*.mp3"] }, new(Strings.Vorbis_files) { Patterns = ["*.ogg"] }]);
        if (openFile is not null)
        {
            ProgressDialogViewModel tracker = new(Strings.Replace_voiced_line);
            VcePlayer.Stop();
            tracker.InitializeTasks(() => _vce.Replace(openFile.Path.LocalPath, _project.BaseDirectory, _project.IterativeDirectory, Path.Combine(_project.Config.CachesDirectory, "vce", $"{_vce.Name}.wav"), _log,
                    _voiceMapEntry),
                () => { });
            await new ProgressDialog { DataContext = tracker }.ShowDialog(Window.Window);
            VcePlayer.Stop();
        }
    }

    private async Task Export()
    {
        IStorageFile saveFile = await Window.Window.ShowSaveFilePickerAsync(Strings.Save_voiced_line_as_WAV, [new(Strings.WAV_File) { Patterns = ["*.wav"] }]);
        if (saveFile is not null)
        {
            WaveFileWriter.CreateWaveFile(saveFile.Path.LocalPath, _vce.GetWaveProvider(_log));
        }
    }

    private void Restore()
    {
        VcePlayer.Stop();
        File.Copy(Path.Combine(_project.BaseDirectory, "original", "vce", Path.GetFileName(_vce.VoiceFile)), Path.Combine(_project.BaseDirectory, _vce.VoiceFile), true);
        File.Copy(Path.Combine(_project.IterativeDirectory, "original", "vce", Path.GetFileName(_vce.VoiceFile)), Path.Combine(_project.IterativeDirectory, _vce.VoiceFile), true);
        AdxHeader header = new(File.ReadAllBytes(Path.Combine(_project.IterativeDirectory, _vce.VoiceFile)), _log);
        if (_voiceMapEntry is not null)
        {
            _voiceMapEntry.Timer = (int)((double)header.TotalSamples / header.SampleRate * 180 + 30);
            _vce.UnsavedChanges = true;
        }
        VcePlayer.Stop();
    }

    private void UpdatePreview()
    {
        SubtitlesPreview = new(256, 384);
        SKCanvas canvas = new(SubtitlesPreview);
        canvas.DrawColor(SKColors.DarkGray);
        canvas.DrawLine(new() { X = 0, Y = 192 }, new() { X = 256, Y = 192 }, DialogueScriptParameter.Paint00);

        if (_voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.BOTTOM)
        {
            for (int i = 0; i <= 1; i++)
            {
                canvas.DrawHaroohieText(
                    _subtitle,
                    DialogueScriptParameter.Paint07,
                    _project,
                    i + _voiceMapEntry.X,
                    1 + _voiceMapEntry.Y + 192,
                    false
                );
            }
        }
        else if (_voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.TOP_FORCE_SHADOW)
        {
            canvas.DrawHaroohieText(
                _subtitle,
                DialogueScriptParameter.Paint07,
                _project,
                1 + _voiceMapEntry.X,
                1 + _voiceMapEntry.Y,
                false
            );
        }

        canvas.DrawHaroohieText(
            _subtitle,
            _voiceMapEntry.Color switch
            {
                DialogueColor.WHITE => DialogueScriptParameter.Paint00,
                DialogueColor.YELLOW => DialogueScriptParameter.Paint01,
                DialogueColor.OFF_WHITE => DialogueScriptParameter.Paint02,
                DialogueColor.GRAY => DialogueScriptParameter.Paint03,
                DialogueColor.LAVENDER => DialogueScriptParameter.Paint04,
                DialogueColor.RED => DialogueScriptParameter.Paint05,
                DialogueColor.FADED_GRAY => DialogueScriptParameter.Paint06,
                DialogueColor.BLACK => DialogueScriptParameter.Paint07,
                _ => DialogueScriptParameter.Paint00,
            },
            _project,
            _voiceMapEntry.X,
            _voiceMapEntry.Y + (_voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.BOTTOM ? 192 : 0),
            false
        );

        canvas.Flush();
    }
}

public class LocalizedDialogueColor(DialogueColor color) : ReactiveObject
{
    [Reactive]
    public DialogueColor Color { get; set; } = color;

    public string DisplayText => Strings.ResourceManager.GetString(Color.ToString());
}
