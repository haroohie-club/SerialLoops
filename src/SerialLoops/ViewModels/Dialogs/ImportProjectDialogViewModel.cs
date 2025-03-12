using System.IO;
using System.IO.Compression;
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
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class ImportProjectDialogViewModel : ViewModelBase
{
    private string _expectedRomHash = string.Empty;
    private string _actualRomHash = string.Empty;
    private ILogger _log;

    [Reactive]
    public string RomHashString { get; set; }
    [Reactive]
    public string SlzipPath { get; set; }
    [Reactive]
    public string RomPath { get; set; }
    [Reactive]
    public bool IgnoreHash { get; set; }
    [Reactive]
    public bool CanImport { get; set; }

    public ICommand SelectExportedProjectCommand { get; }
    public ICommand OpenRomCommand { get; }
    public ICommand IgnoreHashCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand CancelCommand { get; }

    public ImportProjectDialogViewModel(string slzipPath, ILogger log)
    {
        _log = log;

        RomHashString = string.Format(Strings.Expected_ROM_SHA_1_Hash___0_,
            Strings.Select_an_exported_project_to_see_expected_ROM_hash);
        SlzipPath = string.IsNullOrEmpty(slzipPath) ? Strings.No_exported_project_selected : slzipPath;
        if (!string.IsNullOrEmpty(slzipPath))
        {
            SetExpectedRomHash();
        }
        RomPath = Strings.None_Selected;

        SelectExportedProjectCommand = ReactiveCommand.CreateFromTask<ImportProjectDialog>(SelectExportedProject);
        OpenRomCommand = ReactiveCommand.CreateFromTask<ImportProjectDialog>(OpenRom);
        // When we receive this command, the checkbox hasn't changed yet, so we invert the boolean
        IgnoreHashCommand = ReactiveCommand.Create(() => CanImport = !IgnoreHash || _expectedRomHash.Equals(_actualRomHash));
        ImportCommand = ReactiveCommand.Create<ImportProjectDialog>(dialog => dialog.Close((SlzipPath, RomPath)));
        CancelCommand = ReactiveCommand.Create<ImportProjectDialog>(dialog => dialog.Close());
    }

    private async Task SelectExportedProject(ImportProjectDialog dialog)
    {
        string slzipPath = (await dialog.ShowOpenFilePickerAsync(Strings.Import_Project,
            [new(Strings.Exported_Project) { Patterns = ["*.slzip"] }]))?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(slzipPath))
        {
            SlzipPath = slzipPath;
            SetExpectedRomHash();
            await CompareRomHashes(dialog);
        }
    }

    private void SetExpectedRomHash()
    {
        using FileStream slzipFs = File.OpenRead(SlzipPath);
        using ZipArchive slzip = new(slzipFs);
        _expectedRomHash = slzip.Comment;
        RomHashString = string.Format(Strings.Expected_ROM_SHA_1_Hash___0_, _expectedRomHash);
    }

    private async Task OpenRom(ImportProjectDialog dialog)
    {
        string rom = (await dialog.ShowOpenFilePickerAsync(Strings.Import_Project,
            [new(Strings.NDS_ROM) { Patterns = ["*.nds"] }])).TryGetLocalPath();
        if (!string.IsNullOrEmpty(rom))
        {
            _actualRomHash = string.Join("", SHA1.HashData(File.ReadAllBytes(rom)).Select(b => $"{b:X2}"));
            RomPath = rom;
            await CompareRomHashes(dialog);
        }
    }

    private async Task CompareRomHashes(ImportProjectDialog dialog)
    {
        if (string.IsNullOrEmpty(_expectedRomHash) || string.IsNullOrEmpty(_actualRomHash))
        {
            return;
        }

        if (!_expectedRomHash.Equals(_actualRomHash))
        {
            await dialog.ShowMessageBoxAsync(Strings.ROM_Hash_Mismatch,
                Strings.The_selected_ROM_s_hash_does_not_match_the_expected_ROM_hash__Please_ensure_you_are_using_the_correct_base_ROM__n_nIf_you_wish_to_ignore_this__please_check_the___Ignore_Hash___checkbox_,
                ButtonEnum.Ok, Icon.Warning, _log);
            return;
        }

        CanImport = true;
    }
}
