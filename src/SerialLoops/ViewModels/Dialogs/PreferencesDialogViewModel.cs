using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Factories;
using SerialLoops.Lib.Util;
using SerialLoops.Views.Dialogs;
using SixLabors.Fonts;

namespace SerialLoops.ViewModels.Dialogs;

public class PreferencesDialogViewModel : ViewModelBase
{
    public ICommand SaveCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    private IConfigFactory _configFactory;
    public ConfigUser Configuration { get; set; }
    private ILogger Log { get; set; }
    [Reactive]
    public bool RequireRestart { get; set; }
    public bool Saved { get; set; }
    private PreferencesDialog _preferencesDialog;

    public void Initialize(PreferencesDialog preferencesDialog, ILogger log, IConfigFactory configFactory = null)
    {
        _preferencesDialog = preferencesDialog;
        _configFactory = configFactory ?? new ConfigFactory();
        Configuration = _configFactory.LoadConfig(s => s, log);
        Log = log;

        _preferencesDialog.BuildOptions.InitializeOptions(Strings.Build,
        [
            new FolderOption(_preferencesDialog)
            {
                OptionName = Strings.ConfigOptionLlvmPath,
                Path = Configuration.SysConfig.LlvmPath,
                OnChange = path => Configuration.SysConfig.LlvmPath = path,
            },
            new FileOption(_preferencesDialog)
            {
                OptionName = Strings.ConfigOptionNinjaPath,
                Path = Configuration.SysConfig.NinjaPath,
                OnChange = value => Configuration.SysConfig.NinjaPath = value,
            },
            new FileOption(_preferencesDialog)
            {
                OptionName = Strings.PreferencesEmulatorPathLabel,
                Path = Configuration.SysConfig.EmulatorPath,
                OnChange = path =>
                {
                    Configuration.SysConfig.EmulatorPath = path;
                    if (Configuration.SysConfig.StoreEmulatorPath)
                    {
                        new ConfigEmulator()
                        {
                            EmulatorPath = Configuration.SysConfig.EmulatorPath,
                            EmulatorFlatpak = Configuration.SysConfig.EmulatorFlatpak,
                        }.Write();
                    }
                },
                Enabled = string.IsNullOrEmpty(Configuration.SysConfig.BundledEmulator),
            },
            new TextOption
            {
                OptionName = Strings.PreferencesEmulatorFlatpakLabel,
                Value = Configuration.SysConfig.EmulatorFlatpak,
                OnChange = flatpak =>
                {
                    Configuration.SysConfig.EmulatorFlatpak = flatpak;
                    if (Configuration.SysConfig.StoreEmulatorPath)
                    {
                        new ConfigEmulator()
                        {
                            EmulatorPath = Configuration.SysConfig.EmulatorPath,
                            EmulatorFlatpak = Configuration.SysConfig.EmulatorFlatpak,
                        }.Write();
                    }
                },
                Enabled = OperatingSystem.IsLinux() && string.IsNullOrEmpty(Configuration.SysConfig.BundledEmulator),
            },
        ]);
        _preferencesDialog.ProjectOptions.InitializeOptions(Strings.PreferencesProjectsTab,
        [
            new BooleanOption
            {
                OptionName = Strings.PreferencesAutoReopenLabel,
                Value = Configuration.AutoReopenLastProject,
                OnChange = value => Configuration.AutoReopenLastProject = value,
            },
            new BooleanOption
            {
                OptionName = Strings.PreferencesRememberProjectWorkspace,
                Value = Configuration.RememberProjectWorkspace,
                OnChange = value => Configuration.RememberProjectWorkspace = value,
            },
            new BooleanOption
            {
                OptionName = Strings.PreferencesRemoveMissingProjects,
                Value = Configuration.RemoveMissingProjects,
                OnChange = value => Configuration.RemoveMissingProjects = value,
            },
        ]);
        _preferencesDialog.SerialLoopsOptions.InitializeOptions("Serial Loops",
        [
            new ComboBoxOption(
            [
                GetLanguageComboxBoxOption("de"),
                GetLanguageComboxBoxOption("en-GB"),
                GetLanguageComboxBoxOption("en-US"),
                GetLanguageComboxBoxOption("it"),
                GetLanguageComboxBoxOption("pt-BR"),
                GetLanguageComboxBoxOption("ja"),
                GetLanguageComboxBoxOption("zh-Hans"),
            ])
            {
                OptionName = Strings.Language,
                Value = Strings.Culture.Name,
                OnChange = value =>
                {
                    Strings.Culture = CultureInfo.CurrentCulture;
                    Configuration.CurrentCultureName = value;
                    RequireRestart = true;
                },
            },
            new ComboBoxOption([
                ("", string.Format(Strings.DefaultFontDisplay, Strings.DefaultFont)),
                ..SystemFonts.Collection.Families.Select(f => (f.Name, f.Name)),
            ], font: true)
            {
                OptionName = Strings.PreferencesDisplayFontLabel,
                Value = Configuration.DisplayFont ?? "",
                OnChange = value =>
                {
                    Configuration.DisplayFont = value;
                    RequireRestart = true;
                },
            },
            new BooleanOption
            {
                OptionName = Strings.PreferencesCheckForUpdatesOnStartup,
                Value = Configuration.CheckForUpdates,
                OnChange = value => Configuration.CheckForUpdates = value,
                Enabled = Configuration.SysConfig.UseUpdater,
            },
            new BooleanOption
            {
                OptionName = Strings.PreferencesUsePreReleaseUpdateChannel,
                Value = Configuration.PreReleaseChannel,
                OnChange = value => Configuration.PreReleaseChannel = value,
                Enabled = Configuration.SysConfig.UseUpdater,
            },
        ]);

        SaveCommand = ReactiveCommand.Create(SaveCommand_Executed);
        CancelCommand = ReactiveCommand.Create(_preferencesDialog.Close);
    }

    private void SaveCommand_Executed()
    {
        Configuration.Save(Log);
        if (Configuration.SysConfig.StoreSysConfig)
        {
            Configuration.SysConfig.Save(Path.Combine(Path.GetDirectoryName(Configuration.ConfigPath)!, "sysconfig.json"), Log);
        }
        else if (Configuration.SysConfig.StoreEmulatorPath)
        {
            new ConfigEmulator()
            {
                EmulatorPath = Configuration.SysConfig.EmulatorPath,
                EmulatorFlatpak = Configuration.SysConfig.EmulatorFlatpak,
            }.Write();
        }
        Saved = true;
        _preferencesDialog.Close();
    }

    private static (string Culture, string Language) GetLanguageComboxBoxOption(string culture)
    {
        return (culture, new CultureInfo(culture).NativeName.ToSentenceCase());
    }
}
