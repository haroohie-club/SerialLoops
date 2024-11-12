using System;
using System.Globalization;
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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace SerialLoops.ViewModels.Dialogs;

public class PreferencesDialogViewModel : ViewModelBase
{
    public int MinWidth => 550;
    public int MinHeight => 600;
    public int Width { get; set; } = 550;
    public int Height { get; set; } = 600;

    public ICommand SaveCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    private IConfigFactory? _configFactory;
    public Config? Configuration { get; set; }
    public ILogger? Log { get; set; }
    [Reactive]
    public bool RequireRestart { get; set; }
    public bool Saved { get; set; }
    private PreferencesDialog? _preferencesDialog;

    public void Initialize(PreferencesDialog preferencesDialog, ILogger log, IConfigFactory? configFactory = null)
    {
        _preferencesDialog = preferencesDialog;
        if (configFactory is not null)
        {
            _configFactory = configFactory;
        }
        else
        {
            _configFactory = new ConfigFactory();
        }
        Configuration = _configFactory.LoadConfig(s => s, log);
        Log = log;

        _preferencesDialog.BuildOptions.InitializeOptions(Strings.Build,
        [
            new FolderOption(_preferencesDialog)
            {
                OptionName = Strings.devkitARM_Path,
                Path = Configuration!.DevkitArmPath,
                OnChange = (path) => Configuration.DevkitArmPath = path ?? string.Empty,
            },
            new FileOption(_preferencesDialog)
            {
                OptionName = Strings.Emulator_Path,
                Path = Configuration.EmulatorPath,
                OnChange = (path) => Configuration.EmulatorPath = path ?? string.Empty,
            },
            new TextOption
            {
                OptionName = Strings.Emulator_Flatpak,
                Value = Configuration.EmulatorFlatpak,
                OnChange = (flatpak) => Configuration.EmulatorFlatpak = flatpak ?? string.Empty,
                Enabled = OperatingSystem.IsLinux(),
            },
            new BooleanOption
            {
                OptionName = Strings.Use_Docker_for_ASM_Hacks,
                Value = Configuration.UseDocker,
                OnChange = (value) => Configuration.UseDocker = value ?? false,
                Enabled = !OperatingSystem.IsMacOS(),
            },
            new TextOption
            {
                OptionName = Strings.devkitARM_Docker_Tag,
                Value = Configuration.DevkitArmDockerTag,
                OnChange = (value) => Configuration.DevkitArmDockerTag = value ?? string.Empty,
                Enabled = !OperatingSystem.IsMacOS(),
            }
        ]);
        _preferencesDialog.ProjectOptions.InitializeOptions(Strings.Projects,
        [
            new BooleanOption
            {
                OptionName = Strings.Auto_Re_Open_Last_Project,
                Value = Configuration.AutoReopenLastProject,
                OnChange = (value) => Configuration.AutoReopenLastProject = value ?? false,
            },
            new BooleanOption
            {
                OptionName = Strings.Remember_Project_Workspace,
                Value = Configuration.RememberProjectWorkspace,
                OnChange = (value) => Configuration.RememberProjectWorkspace = value ?? false,
            },
            new BooleanOption
            {
                OptionName = Strings.Remove_Missing_Projects,
                Value = Configuration.RemoveMissingProjects,
                OnChange = (value) => Configuration.RemoveMissingProjects = value ?? false,
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
                OnChange = (value) =>
                {
                    Strings.Culture = CultureInfo.CurrentCulture;
                    Configuration.CurrentCultureName = value?? "en";
                    RequireRestart = true;
                },
            },
            new ComboBoxOption([
                ("", string.Format(Strings.Default_Font_Display, Strings.Default_Font)),
                ..SystemFonts.Collection.Families.Select(_ => (_.Name, _.Name)),
            ])
            {
                OptionName = Strings.Display_Font,
                Value = Configuration.DisplayFont ?? "",
                OnChange = (value) =>
                {
                    Configuration.DisplayFont = value ?? Strings.Default_Font;
                    RequireRestart = true;
                },
            },
            new BooleanOption
            {
                OptionName = Strings.Check_for_Updates_on_Startup,
                Value = Configuration.CheckForUpdates,
                OnChange = (value) => Configuration.CheckForUpdates = value ?? false,
            },
            new BooleanOption
            {
                OptionName = Strings.Use_Pre_Release_Update_Channel,
                Value = Configuration.PreReleaseChannel,
                OnChange = (value) => Configuration.PreReleaseChannel = value ?? false,
            }
        ]);

        SaveCommand = ReactiveCommand.Create(SaveCommand_Executed);
        CancelCommand = ReactiveCommand.Create(_preferencesDialog.Close);
    }

    public void SaveCommand_Executed()
    {
        Configuration!.Save(Log!);
        Saved = true;
        _preferencesDialog!.Close();
    }

    private static (string Culture, string Language) GetLanguageComboxBoxOption(string culture)
    {
        return (culture, new CultureInfo(culture).NativeName.ToSentenceCase());
    }
}
