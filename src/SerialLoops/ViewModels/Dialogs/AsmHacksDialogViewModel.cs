using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using HaroohieClub.NitroPacker.Patcher.Nitro;
using HaroohieClub.NitroPacker.Patcher.Overlay;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Hacks;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class AsmHacksDialogViewModel : ViewModelBase
{
    private const int NUM_OVERLAYS = 26;

    private ILogger _log;
    private Project _project;
    public Config Configuration { get; set; }
    private Dictionary<HackFile, SelectedHackParameter[]> _hackParameters { get; set; } = [];
    [Reactive]
    public AsmHack SelectedHack { get; set; }
    public ICommand HackChangedCommand { get; set; }
    public ICommand ImportHackCommand { get; set; }
    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    public AsmHacksDialogViewModel(Project project, Config config, ILogger log)
    {
        _log = log;
        _project = project;
        Configuration = config;
        HackChangedCommand = ReactiveCommand.Create<StackPanel>(HackChangedCommand_Executed);
        ImportHackCommand = ReactiveCommand.CreateFromTask<AsmHacksDialog>(ImportHackCommand_Executed);
        SaveCommand = ReactiveCommand.CreateFromTask<AsmHacksDialog>(SaveCommand_Executed);
        CancelCommand = ReactiveCommand.Create<AsmHacksDialog>((dialog) => dialog.Close());

        DetermineHackParameters();
        Configuration.UpdateHackAppliedStatus(_project, log);
    }

    private void DetermineHackParameters()
    {
        foreach (HackFile file in Configuration.Hacks.SelectMany(h => h.Files).Distinct())
        {
            try
            {
                _hackParameters.Add(file, file.Parameters.Select(p => new SelectedHackParameter { Parameter = p, Selection = 0 }).ToArray());
            }
            catch (Exception ex)
            {
                _log.LogException(string.Format(Strings.Failed_to_add_parameters_for_hack_file__0__in_hack__1_, file.File, SelectedHack.Name), ex);
            }
        }
    }

    public void HackChangedCommand_Executed(StackPanel descriptionPanel)
    {
        descriptionPanel.Children.Clear();
        descriptionPanel.Children.Add(ControlGenerator.GetTextHeader(SelectedHack.Name));
        descriptionPanel.Children.Add(new TextBlock { Text = SelectedHack.Description, TextWrapping = TextWrapping.Wrap });
        StackPanel parametersLayout = new() { Spacing = 5, Margin = new(3) };
        foreach (HackFile file in SelectedHack.Files)
        {
            for (int i = 0; i < _hackParameters[file].Length; i++)
            {
                int currentParam = i; // need this as i increments and will mess up the SelectionChanged method
                ComboBox parameterComboBox = new();
                parameterComboBox.Items.AddRange(_hackParameters[file][currentParam].Parameter.Values.Select(v => new ComboBoxItem { Tag = file, Content = v.Name }));
                parameterComboBox.SelectedIndex = _hackParameters[file][currentParam].Selection;
                parameterComboBox.SelectionChanged += (sender, args) =>
                {
                    SelectedHack.ValueChanged = true;
                    _hackParameters[file][currentParam].Selection = parameterComboBox.SelectedIndex;
                };
                parametersLayout.Children.Add(parameterComboBox);
            }
        }
        GroupBox.Avalonia.Controls.GroupBox paramtersBox = new() { Header = Strings.Parameters, Content = parametersLayout };
        paramtersBox.Theme = (ControlTheme)paramtersBox.FindResource("GroupBoxClassic");
        descriptionPanel.Children.Add(paramtersBox);
    }

    public async Task ImportHackCommand_Executed(AsmHacksDialog dialog)
    {
        IStorageFile file = await dialog.ShowOpenFilePickerAsync(Strings.Import_a_Hack, [new(Strings.Serialized_ASM_hack) { Patterns = ["*.slhack"] }]);
        if (file is not null)
        {
            AsmHack hack = JsonSerializer.Deserialize<AsmHack>(File.ReadAllText(file.Path.LocalPath));

            if (Configuration.Hacks.Any(h => h.Files.Any(f => hack.Files.Contains(f))))
            {
                _log.LogError(Strings.Error__duplicate_hack_detected__A_file_with_the_same_name_as_a_file_in_this_hack_has_already_been_imported_);
                return;
            }
            else if (Configuration.Hacks.Contains(hack))
            {
                _log.LogError("Error: duplicate hack detected! A hack with the same name has already been imported.");
                return;
            }

            foreach (HackFile hackFile in hack.Files)
            {
                File.Copy(Path.Combine(Path.GetDirectoryName(file.Path.LocalPath), hackFile.File), Path.Combine(Configuration.HacksDirectory, hackFile.File));
            }

            hack.IsApplied = hack.Applied(_project);
            Configuration.Hacks.Add(hack);
            Lib.IO.WriteStringFile(Path.Combine(Configuration.HacksDirectory, "hacks.json"), JsonSerializer.Serialize(Configuration.Hacks), _log);
            DetermineHackParameters();
        }
    }

    public async Task SaveCommand_Executed(AsmHacksDialog dialog)
    {
        List<AsmHack> appliedHacks = [];
        List<AsmHack> alreadyAppliedHacks = [];
        foreach (AsmHack hack in Configuration.Hacks)
        {
            bool alreadyApplied = hack.Applied(_project);

            if (alreadyApplied && !hack.IsApplied)
            {
                hack.Revert(_project, _log);
            }
            else if (alreadyApplied && !hack.ValueChanged)
            {
                alreadyAppliedHacks.Add(hack);
            }
            else if (hack.IsApplied)
            {
                hack.Apply(_project, Configuration, _hackParameters.Where(kv => hack.Files.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value), _log, forceApplication: true);
                appliedHacks.Add(hack);
            }
            hack.ValueChanged = false;
        }

        // Write the symbols file based on what the hacks say they need
        File.WriteAllLines(Path.Combine(_project.BaseDirectory, "src", "symbols.x"), appliedHacks.Concat(alreadyAppliedHacks).SelectMany(h => h.Files.Where(f => !f.Destination.Contains("overlays", StringComparison.OrdinalIgnoreCase)).SelectMany(f => f.Symbols)));
        for (int i = 0; i < NUM_OVERLAYS; i++)
        {
            if (appliedHacks.Concat(alreadyAppliedHacks).Any(h => h.Files.Any(f => f.Destination.Contains($"main_{i:X4}", StringComparison.OrdinalIgnoreCase))))
            {
                File.WriteAllLines(Path.Combine(_project.BaseDirectory, "src", "overlays", $"main_{i:X4}", "symbols.x"), appliedHacks.Concat(alreadyAppliedHacks).SelectMany(h => h.Files.Where(f => f.Destination.Contains($"main_{i:X4}", StringComparison.OrdinalIgnoreCase)).SelectMany(f => f.Symbols)));
            }
        }

        // Build and insert ARM9 hacks
        string arm9Path = Path.Combine(_project.BaseDirectory, "src", "arm9.bin");
        ARM9 arm9 = null;
        try
        {
            arm9 = new(File.ReadAllBytes(arm9Path), 0x02000000);
        }
        catch (Exception ex)
        {
            _log.LogException(string.Format(Strings.Failed_to_read_ARM9_from___0__, arm9Path), ex);
        }

        try
        {
            LoopyProgressTracker tracker = new();
            await new ProgressDialog(() =>
            {
                ARM9AsmHack.Insert(Path.Combine(_project.BaseDirectory, "src"), arm9, 0x02005ECC, Configuration.UseDocker ? Configuration.DevkitArmDockerTag : string.Empty,
                    (object sender, DataReceivedEventArgs e) => { _log.Log(e.Data); ((IProgressTracker)tracker).Focus(e.Data, 1); },
                    (object sender, DataReceivedEventArgs e) => _log.LogWarning(e.Data),
                    devkitArmPath: Configuration.DevkitArmPath);
            }, () => { }, tracker, Strings.Patching_ARM9).ShowDialog(dialog);
        }
        catch (Exception ex)
        {
            _log.LogException(Strings.Failed_to_insert_ARM9_assembly_hacks, ex);
        }

        try
        {
            Lib.IO.WriteBinaryFile(Path.Combine("rom", "arm9.bin"), arm9.GetBytes(), _project, _log);
        }
        catch (Exception ex)
        {
            _log.LogException("Failed to write ARM9 to disk", ex);
        }
        if (appliedHacks.All(h => h.Files.Any(f => f.Destination.Contains("overlays", StringComparison.OrdinalIgnoreCase))))
        {
            // Overlay compilation relies on the presence of an arm9_newcode.x containing the symbols of the newly compiled ARM9 code
            // If there is no ARM9 code, we'll need to provide an empty file
            foreach (string overlayDirectory in Directory.GetDirectories(Path.Combine(_project.BaseDirectory, "src", "overlays")))
            {
                File.WriteAllText(Path.Combine(overlayDirectory, "arm9_newcode.x"), string.Empty);
            }
        }

        // Get the overlays
        List<Overlay> overlays = [];
        string originalOverlaysDir = Path.Combine(_project.BaseDirectory, "original", "overlay");
        string romInfoPath = Path.Combine(_project.BaseDirectory, "original", $"{_project.Name}.xml");
        string newRomInfoPath = Path.Combine(_project.BaseDirectory, "rom", $"{_project.Name}.xml");

        foreach (string file in Directory.GetFiles(originalOverlaysDir))
        {
            overlays.Add(new(file, romInfoPath));
        }

        // Patch the overlays
        string overlaySourceDir = Path.Combine(_project.BaseDirectory, "src", "overlays");
        for (int i = 0; i < overlays.Count; i++)
        {
            if (Directory.GetDirectories(overlaySourceDir).Contains(Path.Combine(overlaySourceDir, overlays[i].Name)))
            {
                // If the overlay directory is empty, we've reverted all the hacks in it and should clean it up
                if (Directory.GetFiles(Path.Combine(overlaySourceDir, overlays[i].Name, "source")).Length == 0)
                {
                    Directory.Delete(Path.Combine(overlaySourceDir, overlays[i].Name), true);
                }
                else
                {
                    try
                    {
                        LoopyProgressTracker tracker = new();
                        await new ProgressDialog(() =>
                        {
                            OverlayAsmHack.Insert(overlaySourceDir, overlays[i], newRomInfoPath, Configuration.UseDocker ? Configuration.DevkitArmDockerTag : string.Empty,
                                (object sender, DataReceivedEventArgs e) => { _log.Log(e.Data); ((IProgressTracker)tracker).Focus(e.Data, 1); },
                                (object sender, DataReceivedEventArgs e) => _log.LogWarning(e.Data),
                                devkitArmPath: Configuration.DevkitArmPath);
                        }, () => { }, tracker, string.Format(Strings.Patching_Overlay__0_, overlays[i].Name)).ShowDialog(dialog);
                    }
                    catch (Exception ex)
                    {
                        _log.LogException(string.Format(Strings.Failed_to_insert_hacks_into_overlay__0__, overlays[i].Name), ex);
                    }
                }
            }
        }

        // Save all the overlays in case we've reverted all hacks on one
        foreach (Overlay overlay in overlays)
        {
            try
            {
                overlay.Save(Path.Combine(_project.BaseDirectory, "rom", "overlay", $"{overlay.Name}.bin"));
                File.Copy(Path.Combine(_project.BaseDirectory, "rom", "overlay", $"{overlay.Name}.bin"),
                    Path.Combine(_project.IterativeDirectory, "rom", "overlay", $"{overlay.Name}.bin"), true);
                _project.Settings.File.RomInfo.ARM9Ovt.First(o => o.Id == overlay.Id).RamSize = (uint)overlay.Length;
            }
            catch (Exception ex)
            {
                _log.LogException(string.Format(Strings.Failed_saving_overlay__0__to_disk, overlay.Name), ex);
            }
        }

        // We don't provide visible errors during the compilation of the hacks because it will deadlock the threads
        // So at the end, we should check if any of the hacks that were supposed to be applied are not applied,
        // and if there are some then we should let the user know.
        IEnumerable<string> failedHackNames = appliedHacks.Where(h => !h.Applied(_project)).Select(h => h.Name);
        if (failedHackNames.Any())
        {
            _log.LogError(string.Format(Strings.Failed_to_apply_the_following_hacks_to_the_ROM__n_0__n_nPlease_check_the_log_file_for_more_information__n_nIn_order_to_preserve_state__no_hacks_were_applied_, string.Join(", ", failedHackNames)));
            foreach (AsmHack hack in appliedHacks)
            {
                hack.Revert(_project, _log);
            }
            IEnumerable<string> dirsToDelete = Directory.GetDirectories(Path.Combine(_project.BaseDirectory, "src"), "-p", SearchOption.AllDirectories)
                .Concat(Directory.GetDirectories(Path.Combine(_project.BaseDirectory, "src"), "build", SearchOption.AllDirectories));
            foreach (string dir in dirsToDelete)
            {
                Directory.Delete(dir, recursive: true);
            }
            string[] filesToDelete = Directory.GetFiles(Path.Combine(_project.BaseDirectory, "src"), "arm9_newcode.x", SearchOption.AllDirectories);
            foreach (string file in filesToDelete)
            {
                File.Delete(file);
            }
        }
        else
        {
            if (appliedHacks.Count != 0)
            {
                await dialog.ShowMessageBoxAsync(Strings.Successfully_applied_hacks_, string.Format(Strings.Successfully_applied_the_following_hacks__n_0_, string.Join(", ", appliedHacks.Select(h => h.Name))),
                    ButtonEnum.Ok, Icon.Info, _log);
            }
            else
            {
                await dialog.ShowMessageBoxAsync(Strings.Success_, Strings.No_hacks_applied_, ButtonEnum.Ok, Icon.Info, _log);
            }
        }

        dialog.Close();
    }
}