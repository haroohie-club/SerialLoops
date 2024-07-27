﻿using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using SerialLoops.Views.Dialogs;
using System;
using System.Globalization;
using System.Windows.Input;

namespace SerialLoops.ViewModels.Dialogs
{
    public class PreferencesDialogViewModel : ViewModelBase
    {
        public int MinWidth => 600;
        public int MinHeight => 800;
        public int Width { get; set; } = 600;
        public int Height { get; set; } = 800;
        public int ScrollViewerHeight => Height - 100;

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public Config Configuration { get; set; }
        public ILogger Log { get; set; }
        public bool RequireRestart { get; set; }
        public bool Saved { get; set; }
        private PreferencesDialog _preferencesDialog;

        public void Initialize(PreferencesDialog preferencesDialog, Config config, ILogger log)
        {
            _preferencesDialog = preferencesDialog;
            Configuration = config;
            Log = log;

            _preferencesDialog.BuildOptions.InitializeOptions(Strings.Build,
                [
                    new FolderOption(_preferencesDialog)
                    {
                        OptionName = Strings.devkitARM_Path,
                        Path = Configuration.DevkitArmPath,
                        OnChange = (path) => Configuration.DevkitArmPath = path,
                    },
                    new FileOption(_preferencesDialog)
                    {
                        OptionName = Strings.Emulator_Path,
                        Path = Configuration.EmulatorPath,
                        OnChange = (path) => Configuration.EmulatorPath = path,
                    },
                    new BooleanOption
                    {
                        OptionName = Strings.Use_Docker_for_ASM_Hacks,
                        Value = Configuration.UseDocker,
                        OnChange = (value) => Configuration.UseDocker = value,
                        Enabled = !OperatingSystem.IsMacOS(),
                    },
                    new TextOption
                    {
                        OptionName = Strings.devkitARM_Docker_Tag,
                        Value = Configuration.DevkitArmDockerTag,
                        OnChange = (value) => Configuration.DevkitArmDockerTag = value,
                        Enabled = !OperatingSystem.IsMacOS(),
                    }
                ]);
            _preferencesDialog.ProjectOptions.InitializeOptions(Strings.Projects,
                [
                    new BooleanOption
                    {
                        OptionName = Strings.Auto_Re_Open_Last_Project,
                        Value = Configuration.AutoReopenLastProject,
                        OnChange = (value) => Configuration.AutoReopenLastProject = value,
                    },
                    new BooleanOption
                    {
                        OptionName = Strings.Remember_Project_Workspace,
                        Value = Configuration.RememberProjectWorkspace,
                        OnChange = (value) => Configuration.RememberProjectWorkspace = value,
                    },
                    new BooleanOption
                    {
                        OptionName = Strings.Remove_Missing_Projects,
                        Value = Configuration.RemoveMissingProjects,
                        OnChange = (value) => Configuration.RemoveMissingProjects = value,
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
                            Configuration.CurrentCultureName = value;
                            RequireRestart = true;
                        },
                    },
                    new BooleanOption
                    {
                        OptionName = Strings.Check_for_Updates_on_Startup,
                        Value = Configuration.CheckForUpdates,
                        OnChange = (value) => Configuration.CheckForUpdates = value,
                    },
                    new BooleanOption
                    {
                        OptionName = Strings.Use_Pre_Release_Update_Channel,
                        Value = Configuration.PreReleaseChannel,
                        OnChange = (value) => Configuration.PreReleaseChannel = value,
                    }
                ]);

            SaveCommand = ReactiveCommand.Create(SaveCommand_Executed);
            CancelCommand = ReactiveCommand.Create(_preferencesDialog.Close);
        }

        public void SaveCommand_Executed()
        {
            Configuration.Save(Log);
            Saved = true;
            _preferencesDialog.Close();
        }

        private static (string Culture, string Language) GetLanguageComboxBoxOption(string culture)
        {
            return (culture, new CultureInfo(culture).NativeName.ToTitleCase());
        }
    }
}