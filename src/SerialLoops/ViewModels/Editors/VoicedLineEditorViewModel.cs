using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Audio.ADX;
using HaruhiChokuretsuLib.Util;
using NAudio.Wave;
using ReactiveHistory;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
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
    public ICommand ReplaceAsAhxCommand { get; set; }
    public ICommand ExportCommand { get; set; }
    public ICommand RestoreCommand { get; set; }

    [Reactive]
    public SoundPlayerPanelViewModel VcePlayer { get; set; }
    public ScreenSelectorViewModel ScreenSelector { get; set; }
    [Reactive]
    public SKBitmap SubtitlesPreview { get; set; } = new(256, 384);

    public DialogueColorPalette DialogueColorPalette { get; }
    private int _subtitleColor;
    public Color SubtitleColor
    {
        get => _project.DialogueColors[_subtitleColor].ToAvalonia();
        set
        {
            this.RaiseAndSetIfChanged(ref _subtitleColor, _project.DialogueColors.Select(c => c.ToAvalonia()).ToList().IndexOf(value));
            if (_voiceMapEntry is not null)
            {
                _voiceMapEntry.Color = (DialogueColor)_subtitleColor;
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
    public ObservableCollection<LocalizedSubtitlePosition> SubtitlePositions { get; } =
        new(Enum.GetValues<VoiceMapEntry.YPosition>().Select(p => new LocalizedSubtitlePosition(p)));

    public LocalizedSubtitlePosition SubtitlePosition
    {
        get => SubtitlePositions.FirstOrDefault(p => p.Position == _yPos);
        set
        {
            this.RaiseAndSetIfChanged(ref _yPos, value.Position);
            _voiceMapEntry.YPos = _yPos;
            UpdatePreview();
            Description.UnsavedChanges = true;
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

    private StackHistory _history;

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public KeyGesture UndoGesture { get; }
    public KeyGesture RedoGesture { get; }

    public VoicedLineEditorViewModel(VoicedLineItem item, MainWindowViewModel window, ILogger log) : base(item, window, log, window.OpenProject)
    {
        _history = new();

        _vce = item;
        VcePlayer = new(_vce, log, null);
        VcePlayer.TrackDetails = _vce.AdxType.ToString();
        ReplaceCommand = ReactiveCommand.CreateFromTask(() => Replace(false));
        ReplaceAsAhxCommand = ReactiveCommand.CreateFromTask(() => Replace(true));
        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        RestoreCommand = ReactiveCommand.Create(Restore);

        ScreenSelector = new(DsScreen.BOTTOM, false);
        ScreenSelector.ScreenChanged += (sender, args) =>
        {
            SubtitleScreen = ScreenSelector.SelectedScreen;
        };

        _voiceMapEntry = _project.VoiceMap?.VoiceMapEntries.FirstOrDefault(v => v.VoiceFileName.Equals(Path.GetFileNameWithoutExtension(_vce.VoiceFile)));
        if (_voiceMapEntry is not null)
        {
            DialogueColorPalette = new(_project);
            _subtitleColor = (int)_voiceMapEntry.Color == 100 ? 0 : (int)_voiceMapEntry.Color;
            _subtitle = _voiceMapEntry.Subtitle;
            _subtitleScreen = _voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.BOTTOM ? DsScreen.BOTTOM : DsScreen.TOP;
            _forceDropShadow = _voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.TOP_FORCE_SHADOW;
            _yPos = _voiceMapEntry.YPos;
            UpdatePreview();

            this.WhenAnyValue(v => v.SubtitleColor).ObserveWithHistory(c => SubtitleColor = c, SubtitleColor, _history);
            this.WhenAnyValue(v => v.SubtitleScreen).ObserveWithHistory(s => SubtitleScreen = s, SubtitleScreen, _history);
            this.WhenAnyValue(v => v.ForceDropShadow).ObserveWithHistory(d => ForceDropShadow = d, ForceDropShadow, _history);
            this.WhenAnyValue(v => v.SubtitlePosition).ObserveWithHistory(p => SubtitlePosition = p, SubtitlePosition, _history);
        }

        UndoCommand = ReactiveCommand.Create(() => _history.Undo());
        RedoCommand = ReactiveCommand.Create(() => _history.Redo());
        UndoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Z);
        RedoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Y);
    }

    private async Task Replace(bool asAhx)
    {
        IStorageFile openFile = await Window.Window.ShowOpenFilePickerAsync(Strings.VoicedLineEditorReplaceLine, [new(Strings.FiletypeSupportedAudio) { Patterns = Shared.SupportedAudioFiletypes },
            new(Strings.FiletypeWavs) { Patterns = ["*.wav"] }, new(Strings.FiletypeFlac) { Patterns = ["*.flac"] },
            new(Strings.FiletypeMp3) { Patterns = ["*.mp3"] }, new(Strings.FiletypeOgg) { Patterns = ["*.ogg"] }]);
        if (openFile is not null)
        {
            ProgressDialogViewModel tracker = new(Strings.VoicedLineEditorReplaceLine);
            VcePlayer.Stop();
            tracker.InitializeTasks(() => _vce.Replace(openFile.Path.LocalPath, _project, Path.Combine(_project.ConfigUser.CachesDirectory, "vce", $"{_vce.Name}.wav"), _log,
                    _voiceMapEntry),
                () => { });
            await new ProgressDialog { DataContext = tracker }.ShowDialog(Window.Window);
            VcePlayer.Stop();
            VcePlayer.TrackDetails = _vce.AdxType.ToString();
        }
    }

    private async Task Export()
    {
        IStorageFile saveFile = await Window.Window.ShowSaveFilePickerAsync(Strings.VoicedLineEditorSaveAsWavLabel, [new(Strings.FiletypeWav) { Patterns = ["*.wav"] }]);
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
        using SKCanvas canvas = new(SubtitlesPreview);
        canvas.DrawColor(SKColors.DarkGray);
        canvas.DrawLine(new() { X = 0, Y = 192 }, new() { X = 256, Y = 192 }, _project.DialogueColorFilters[0]);

        if (_voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.BOTTOM)
        {
            for (int i = 0; i <= 1; i++)
            {
                canvas.DrawHaroohieText(
                    _subtitle,
                    _project.DialogueColorFilters[7],
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
                _project.DialogueColorFilters[7],
                _project,
                1 + _voiceMapEntry.X,
                1 + _voiceMapEntry.Y,
                false
            );
        }

        canvas.DrawHaroohieText(
            _subtitle,
            _project.DialogueColorFilters[(int)_voiceMapEntry.Color],
            _project,
            _voiceMapEntry.X,
            _voiceMapEntry.Y + (_voiceMapEntry.TargetScreen == VoiceMapEntry.Screen.BOTTOM ? 192 : 0),
            false
        );

        canvas.Flush();
    }
}

public class LocalizedSubtitlePosition(VoiceMapEntry.YPosition position) : ReactiveObject
{
    [Reactive]
    public VoiceMapEntry.YPosition Position { get; set; } = position;

    public override string ToString() => Strings.ResourceManager.GetString($"Y_{Position}");
}
