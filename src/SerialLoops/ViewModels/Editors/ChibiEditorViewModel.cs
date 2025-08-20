using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using DynamicData;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public partial class ChibiEditorViewModel : EditorViewModel
{
    private ChibiItem _chibi;

    [Reactive]
    public AnimatedImageViewModel AnimatedChibi { get; set; }

    [Reactive]
    public ChibiDirectionSelectorViewModel DirectionSelector { get; set; }

    private ChibiItem.Direction _selectedDirection;

    public ObservableCollection<string> ChibiAnimationNames { get; } = [];
    private string _selectedAnimation;
    public string SelectedAnimation
    {
        get => _selectedAnimation;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedAnimation, value);
            DirectionSelector.SelectedDirection = DirectionSelector.Directions[2];
            _selectedDirection = DirectionSelector.SelectedDirection.Direction;
            UpdateChibi();
            SelectedAnimToolTip = _selectedAnimation[(_selectedAnimation.Length - 2).._selectedAnimation.Length] switch
            {
                "00" => Strings.ChibiEditorAnim00Help,
                "01" => Strings.ChibiEditorAnim01Help,
                "02" => Strings.ChibiEditorAnim02Help,
                "03" => Strings.ChibiEditorAnim03Help,
                "04" => Strings.ChibiEditorAnim04Help,
                "05" => Strings.ChibiEditorAnim05Help,
                "06" => Strings.ChibiEditorAnim06Help,
                "07" => Strings.ChibiEditorAnim07Help,
                "08" => Strings.ChibiEditorAnim08Help,
                "10" => Strings.ChibiEditorAnim10Help,
                "97" => Strings.ChibiEditorAnim97Help,
                "98" => Strings.ChibiEditorAnim98Help,
                "99" => Strings.ChibiEditorAnim99Help,
                _ => string.Empty,
            };
        }
    }
    [Reactive]
    public string SelectedAnimToolTip { get; set; } = Strings.ChibiEditorAnim00Help;

    public ObservableCollection<ReactiveFrameWithTiming> ChibiFrames { get; } = [];

    public ICommand ExportFramesCommand { get; }
    public ICommand ExportGifCommand { get; }
    public ICommand ReplaceFramesCommand { get; }

    public ChibiEditorViewModel(ChibiItem chibi, MainWindowViewModel window, ILogger log) : base(chibi, window, log)
    {
        _chibi = chibi;

        DirectionSelector = new(ChibiItem.Direction.DOWN_LEFT, direction =>
        {
            _selectedDirection = direction;
            AnimatedChibi = new(_chibi.ChibiAnimations[$"{_selectedAnimation}{ChibiItem.DirectionToCode(_selectedDirection)}"],
                () => Description.UnsavedChanges = true);
            ChibiFrames.Clear();
            ChibiFrames.AddRange(AnimatedChibi.FramesWithTimings);
        });

        ChibiAnimationNames.AddRange(_chibi.ChibiEntries.Select(e => e.Name[..^3]).Distinct());
        _selectedAnimation = ChibiAnimationNames[0];
        UpdateChibi();

        ExportFramesCommand = ReactiveCommand.CreateFromTask(ExportFrames);
        ExportGifCommand = ReactiveCommand.CreateFromTask(ExportGif);
        ReplaceFramesCommand = ReactiveCommand.CreateFromTask(ReplaceFrames);
    }

    private void UpdateChibi()
    {
        DirectionSelector.SetAvailableDirections(_chibi.ChibiEntries.Where(c => c.Name.StartsWith(_selectedAnimation)).ToList());
        AnimatedChibi = new(_chibi.ChibiAnimations[$"{_selectedAnimation}{ChibiItem.DirectionToCode(_selectedDirection)}"],
            () => Description.UnsavedChanges = true);
        ChibiFrames.Clear();
        ChibiFrames.AddRange(AnimatedChibi.FramesWithTimings);
    }

    private async Task ExportFrames()
    {
        string exportFolder = (await Window.Window.ShowOpenFolderPickerAsync(Strings.ChibiEditorSelectExportFolder))?.TryGetLocalPath();
        if (string.IsNullOrEmpty(exportFolder))
        {
            return;
        }

        if (!Directory.Exists(exportFolder))
        {
            Directory.CreateDirectory(exportFolder);
        }

        for (int i = 0; i < ChibiFrames.Count; i++)
        {
            try
            {
                await using FileStream fs = File.Create(Path.Combine(exportFolder,
                    $"{_chibi.DisplayName}_{_selectedAnimation}{ChibiItem.DirectionToCode(_selectedDirection)}_{i:D3}_{ChibiFrames[i].Timing}f.png"));
                ChibiFrames[i].Frame.Encode(fs, SKEncodedImageFormat.Png, 1);
            }
            catch (Exception ex)
            {
                _log.LogException(string.Format(Strings.ErrorFailedExportingChibiAnimation, i, _chibi.DisplayName), ex);
            }
        }
        await Window.Window.ShowMessageBoxAsync(Strings.MessageBoxTitleSuccessGeneric, Strings.ChibiEditorFramesExportedMessage, ButtonEnum.Ok, Icon.Success, _log);
    }

    private async Task ExportGif()
    {
        string savedGif = (await Window.Window.ShowSaveFilePickerAsync(Strings.ChibiEditorSaveGifFileDialogTitle, [new(Strings.FiletypeGif) { Patterns = ["*.gif"] }]))?.TryGetLocalPath();
        if (string.IsNullOrEmpty(savedGif))
        {
            return;
        }

        List<SKBitmap> frames = [];
        foreach (ReactiveFrameWithTiming frame in ChibiFrames)
        {
            for (int i = 0; i < frame.Timing; i++)
            {
                frames.Add(frame.Frame);
            }
        }

        ProgressDialogViewModel tracker = new(Strings.AnimationExportingGifProgressMessage);
        tracker.InitializeTasks(() => frames.SaveGif(savedGif, tracker), async void () => await Window.Window.ShowMessageBoxAsync(Strings.MessageBoxTitleSuccessGeneric, Strings.AnimationExportedGifSuccessMessage, ButtonEnum.Ok, Icon.Success, _log));
        await new ProgressDialog { DataContext = tracker }.ShowDialog(Window.Window);
    }

    private async Task ReplaceFrames()
    {
        IReadOnlyList<IStorageFile> importedFiles = await Window.Window.ShowOpenMultiFilePickerAsync(Strings.AnimationSelectFramesReplacementDialogTitle, [ new(Strings.FiletypeImages) { Patterns = Shared.SupportedImageFiletypes } ]);
        if (importedFiles is null || importedFiles.Count == 0)
        {
            return;
        }

        string[] files = importedFiles.Select(f => f.TryGetLocalPath()).ToArray();
        List<SKBitmap> frames = files.Select(SKBitmap.Decode).ToList();
        short[] timings = new short[frames.Count];
        List<ReactiveFrameWithTiming> framesWithTimings = [];
        for (int i = 0; i < timings.Length; i++)
        {
            Match match = TimingRegex().Match(files[i]);
            if (match.Success)
            {
                timings[i] = short.Parse(match.Groups["timing"].Value);
            }
            else
            {
                timings[i] = 32;
            }
            framesWithTimings.Add(new(frames[i], timings[i], () => Description.UnsavedChanges = true));
        }

        AnimatedChibi = new(frames.Zip(timings), () => Description.UnsavedChanges = true);
        ChibiFrames.Clear();
        ChibiFrames.AddRange(framesWithTimings);
    }

    [GeneratedRegex(@"_(?<timing>\d+)f")]
    private static partial Regex TimingRegex();
}
