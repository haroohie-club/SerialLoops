using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class ExportPatchDialogViewModel
{
    private Project _project;
    private ILogger _log;
    private const string EXPECTED_HASH = "81D5C6316DBCEF9F4C51984ADCAAE171124EBB08";

    [Reactive]
    public string RomPath { get; set; }
    [Reactive]
    public string BaseRomHash { get; set; }
    [Reactive]
    public string MatchSvgPath { get; set; }
    [Reactive]
    public string XDeltaPath { get; set; }

    public string ExpectedRomHashString { get; } = string.Format(Strings.Expected_ROM_SHA_1_Hash___0_, EXPECTED_HASH);

    public ICommand OpenRomCommand { get; }
    public ICommand SelectXdeltaPathCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    public ExportPatchDialogViewModel(Project project, ILogger log)
    {
        _project = project;
        _log = log;

        OpenRomCommand = ReactiveCommand.CreateFromTask<ExportPatchDialog>(OpenRom);
        SelectXdeltaPathCommand = ReactiveCommand.CreateFromTask<ExportPatchDialog>(SelectXDeltaPath);
        CreateCommand = ReactiveCommand.CreateFromTask<ExportPatchDialog>(CreateXDelta);
        CancelCommand = ReactiveCommand.Create<ExportPatchDialog>(dialog => dialog.Close());
    }

    private async Task OpenRom(ExportPatchDialog dialog)
    {
        string romPath =
            (await dialog.ShowOpenFilePickerAsync(Strings.Select_base_ROM,
                [new(Strings.NDS_ROM) { Patterns = ["*.nds"] }]))?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(romPath))
        {
            RomPath = romPath;
            BaseRomHash = string.Join("", SHA1.HashData(File.ReadAllBytes(romPath)).Select(b => $"{b:X2}"));
            if (BaseRomHash.Equals(EXPECTED_HASH))
            {
                MatchSvgPath = string.Empty; // I'd like to have a green checkmark here
            }
            else
            {
                MatchSvgPath = "avares://SerialLooops/Assets/Icons/Warning.svg"; // Ideally this would be a red X ?
            }
        }
    }

    private async Task SelectXDeltaPath(ExportPatchDialog dialog)
    {
        string xDeltaPath = (await dialog.ShowSaveFilePickerAsync(Strings.XDelta_patch,
            [new(Strings.XDelta_patch) { Patterns = ["*.xdelta"] }], $"{_project.Name}.xdelta"))?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(xDeltaPath))
        {
            XDeltaPath = xDeltaPath;
        }
    }

    private async Task CreateXDelta(ExportPatchDialog dialog)
    {
        if (string.IsNullOrEmpty(RomPath))
        {
            await dialog.ShowMessageBoxAsync(Strings.No_base_ROM_selected_,
                Strings.Please_select_a_base_ROM_before_attempting_to_create_the_XDelta_patch_, ButtonEnum.Ok,
                Icon.Warning, _log);
            return;
        }
        if (string.IsNullOrEmpty(XDeltaPath))
        {
            await dialog.ShowMessageBoxAsync(Strings.No_path_to_XDelta_file_selected_,
                Strings.Please_select_a_path_to_save_the_XDelta_patch_to_before_attempting_to_create_it_, ButtonEnum.Ok,
                Icon.Warning, _log);
            return;
        }

        ProgressDialogViewModel tracker = new(Strings.Creating_Patch);
        tracker.InitializeTasks(
            () => Patch.CreatePatch(RomPath, Path.Combine(_project.MainDirectory, $"{_project.Name}.nds"),
                XDeltaPath, _log),
            () => { });
        try
        {
            await new ProgressDialog { DataContext = tracker }.ShowDialog(dialog);
            await dialog.ShowMessageBoxAsync(Strings.Patch_Created_, Strings.Success_, ButtonEnum.Ok, Icon.Success,
                _log);
        }
        catch (Exception ex)
        {
            _log.LogException(Strings.Failed_to_create_XDelta_patch_, ex);
        }

        dialog.Close();
    }
}
