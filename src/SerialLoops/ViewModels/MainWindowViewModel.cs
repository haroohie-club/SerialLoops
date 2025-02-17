using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using MiniToolbar.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Factories;
using SerialLoops.Lib.Hacks;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.SaveFile;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views;
using SerialLoops.Views.Dialogs;
using SerialLoops.Views.Editors;
using SerialLoops.Views.Panels;
using SkiaSharp;

namespace SerialLoops.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string BASE_TITLE = "Serial Loops";

    public string[] Args { get; set; }

    [Reactive]
    public string Title { get; set; } = BASE_TITLE;
    public Size MinSize => new(769, 420);
    [Reactive]
    public Size ClientSize { get; set; } = new(1200, 800);

    public MainWindow Window { get; set; }
    public ProjectsCache ProjectsCache { get; set; }
    public Config CurrentConfig { get; set; }
    public Project OpenProject { get; set; }
    public OpenProjectPanel ProjectPanel { get; set; }
    public Dictionary<MenuHeader, NativeMenuItem> WindowMenu { get; set; }
    public Toolbar ToolBar => Window.ToolBar;
    public EditorTabsPanelViewModel EditorTabs { get; set; }
    public ItemExplorerPanelViewModel ItemExplorer { get; set; }

    public NativeMenuItem RecentProjectsMenu { get; set; } = new(Strings.Recent_Projects);
    public LoopyLogger Log { get; set; }

    private SKBitmap _blankNameplate, _blankNameplateBaseArrow;
    private SKTypeface _msGothicHaruhi;

    public string ShutdownUpdateUrl { get; set; } = null;

    public ICommand NewProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand OpenRecentProjectCommand { get; }
    public ICommand ImportProjectCommand { get; }
    public ICommand EditSaveCommand { get; }
    public ICommand AboutCommand { get; }
    public ICommand PreferencesCommand { get; }
    public ICommand CheckForUpdatesCommand { get; }
    public ICommand ViewLogsCommand { get; }

    public ICommand SaveProjectCommand { get; }
    public ICommand ProjectSettingsCommand { get; }
    public ICommand MigrateProjectCommand { get; }
    public ICommand ExportProjectCommand { get; }
    public ICommand ExportPatchCommand { get; }
    public ICommand CloseProjectCommand { get; }

    public ICommand ApplyHacksCommand { get; }
    public ICommand CreateAsmHackCommand { get; }
    public ICommand EditUiTextCommand { get; }
    public ICommand EditTutorialMappingsCommand { get; }
    public ICommand SearchProjectCommand { get; }

    public ICommand BuildIterativeCommand { get; }
    public ICommand BuildBaseCommand { get; }
    public ICommand BuildAndRunCommand { get; }

    public SfxMixer SfxMixer { get; } = new();

    [Reactive]
    public KeyGesture SaveHotKey { get; set; }
    [Reactive]
    public KeyGesture SearchHotKey { get; set; }
    [Reactive]
    public KeyGesture CloseProjectKey { get; set; }

    public MainWindowViewModel()
    {
        SaveHotKey = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.S);
        SearchHotKey = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.F);
        CloseProjectKey = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.W);

        NewProjectCommand = ReactiveCommand.CreateFromTask(NewProjectCommand_Executed);
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectCommand_Executed);
        OpenRecentProjectCommand = ReactiveCommand.CreateFromTask<string>(OpenRecentProjectCommand_Executed);
        ImportProjectCommand = ReactiveCommand.CreateFromTask<string>(ImportProjectCommand_Executed);
        EditSaveCommand = ReactiveCommand.CreateFromTask(EditSaveFileCommand_Executed);
        AboutCommand = ReactiveCommand.CreateFromTask(AboutCommand_Executed);
        PreferencesCommand = ReactiveCommand.CreateFromTask(PreferencesCommand_Executed);
        CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(new UpdateChecker(this).Check);

        SaveProjectCommand = ReactiveCommand.Create(SaveProject_Executed);
        SearchProjectCommand = ReactiveCommand.Create(SearchProject_Executed);

        ApplyHacksCommand = ReactiveCommand.CreateFromTask(ApplyHacksCommand_Executed);
        CreateAsmHackCommand = ReactiveCommand.CreateFromTask(CreateAsmHackCommand_Executed);
        EditUiTextCommand = ReactiveCommand.CreateFromTask(EditUiTextCommand_Executed);
        EditTutorialMappingsCommand = ReactiveCommand.CreateFromTask(EditTutorialMappingsCommand_Executed);
        ProjectSettingsCommand = ReactiveCommand.CreateFromTask(ProjectSettingsCommand_Executed);
        ExportProjectCommand = ReactiveCommand.CreateFromTask(ExportProjectCommand_Executed);
        ExportPatchCommand = ReactiveCommand.CreateFromTask(ExportPatchCommand_Executed);
        CloseProjectCommand = ReactiveCommand.CreateFromTask(CloseProjectView);

        BuildIterativeCommand = ReactiveCommand.CreateFromTask(BuildIterative_Executed);
        BuildBaseCommand = ReactiveCommand.CreateFromTask(BuildBase_Executed);
        BuildAndRunCommand = ReactiveCommand.CreateFromTask(BuildAndRun_Executed);

        ViewLogsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(CurrentConfig.UserDirectory, "Logs", "SerialLoops.log"),
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Log.LogException(string.Format(Strings._Failed_to_open_log_file_directly__Logs_can_be_found_at__0__,
                    Path.Combine(CurrentConfig.UserDirectory, "Logs", "SerialLoops.log")), ex);
            }
        });
    }

    public void Initialize(MainWindow window, IConfigFactory configFactory = null)
    {
        Window = window;
        Log = new(Window);
        configFactory ??= new ConfigFactory();
        CurrentConfig = configFactory.LoadConfig((s) => s, Log);
        Strings.Culture = new(CurrentConfig.CurrentCultureName);
        Log.Initialize(CurrentConfig);

        var fontStyle = new Style(x => x.OfType<Window>());
        var font = FontFamily.Parse(string.IsNullOrEmpty(CurrentConfig.DisplayFont) ? Strings.Default_Font : CurrentConfig.DisplayFont);
        fontStyle.Add(new Setter(Avalonia.Controls.Primitives.TemplatedControl.FontFamilyProperty, font));
        Application.Current!.Styles.Add(fontStyle);

        ProjectsCache = ProjectsCache.LoadCache(CurrentConfig, Log);
        UpdateRecentProjects();

        if (CurrentConfig.CheckForUpdates)
        {
            CheckForUpdatesCommand.Execute(null);
        }

        if (Args?.Length > 0)
        {
            if (Args[0].EndsWith(".slproj", StringComparison.OrdinalIgnoreCase))
            {
                OpenRecentProjectCommand.Execute(Args[0]);
            }
            else if (Args[0].EndsWith(".slzip", StringComparison.OrdinalIgnoreCase))
            {
                OpenHomePanel();
                ImportProjectCommand.Execute(Args[0]);
            }
        }
        else if (CurrentConfig.AutoReopenLastProject && ProjectsCache.RecentProjects.Count > 0)
        {
            OpenRecentProjectCommand.Execute(ProjectsCache.RecentProjects[0]);
        }
        else
        {
            OpenHomePanel();
        }
    }

    private void OpenHomePanel()
    {
        HomePanel homePanel = new() { DataContext = new HomePanelViewModel(this) };
        Window.MainContent.Content = homePanel;
    }

    internal void OpenProjectView(Project project, IProgressTracker tracker)
    {
        EditorTabs = new(this, project, Log);
        ItemExplorer = new(SearchProjectCommand, this);
        ProjectPanel = new()
        {
            DataContext = new OpenProjectPanelViewModel(ItemExplorer, EditorTabs),
        };

        InitializeProjectMenu();

        using Stream blankNameplateStream = AssetLoader.Open(new("avares://SerialLoops/Assets/Graphics/BlankNameplate.png"));
        _blankNameplate = SKBitmap.Decode(blankNameplateStream);
        using Stream blankNameplateBaseArrowStream = AssetLoader.Open(new("avares://SerialLoops/Assets/Graphics/BlankNameplateBaseArrow.png"));
        _blankNameplateBaseArrow = SKBitmap.Decode(blankNameplateBaseArrowStream);
        using Stream typefaceStream = AssetLoader.Open(new("avares://SerialLoops/Assets/Graphics/MS-Gothic-Haruhi.ttf"));
        _msGothicHaruhi = SKTypeface.FromStream(typefaceStream);

        Title = $"{BASE_TITLE} - {project.Name}";

        //LoadCachedData(project, tracker);

        Window.MainContent.Content = ProjectPanel;
    }

    public async Task<bool> CloseProject_Executed(WindowClosingEventArgs e)
    {
        bool cancel = false;
        if (OpenProject is not null)
        {
            if (EditorTabs is null)
            {
                // If there are no editor tabs, we're in standalone save editor mode
                Directory.Delete(OpenProject.MainDirectory, true); // Clean up after ourselves
                return false;
            }

            // Warn against unsaved items
            IEnumerable<ItemDescription> unsavedItems = OpenProject.Items.Where(i => i.UnsavedChanges);
            if (unsavedItems.Any())
            {
                ButtonResult result;
                bool skipBuild = false;
                if (e?.CloseReason == WindowCloseReason.OSShutdown) // if the OS is shutting down, we're going to expedite things
                {
                    result = ButtonResult.Yes;
                    skipBuild = true;
                }
                else
                {
                    // message box with yes no cancel buttons
                    result = await Window.ShowMessageBoxAsync(Strings.Confirm, string.Format(Strings.You_have_unsaved_changes_in__0__item_s___Would_you_like_to_save_before_closing_the_project_, unsavedItems.Count()),
                        ButtonEnum.YesNoCancel, Icon.Warning, Log);
                }
                switch (result)
                {
                    case ButtonResult.Yes:
                        SaveProject_Executed();
                        if (!skipBuild)
                        {
                            //BuildIterativeProject_Executed(sender, e); // make sure we lock in the changes
                        }
                        break;
                    default:
                        cancel = true;
                        if (e is not null)
                        {
                            e.Cancel = true;
                        }
                        break;
                }
            }

            // Record open items
            List<string> openItems = EditorTabs.Tabs.Cast<EditorViewModel>()
                .Select(e => e.Description)
                .Select(i => i.Name)
                .ToList();
            ProjectsCache.CacheRecentProject(OpenProject.ProjectFile, openItems);
            ProjectsCache.HadProjectOpenOnLastClose = true;
            ProjectsCache.Save(Log);
        }
        else
        {
            cancel = true;
        }
        return cancel;
    }

    private async Task ApplyHacksCommand_Executed()
    {
        AsmHacksDialogViewModel hacksModel = new(OpenProject, CurrentConfig, Log);
        AsmHacksDialog hacksDialog = new() { DataContext = hacksModel};
        await hacksDialog.ShowDialog(Window);
    }

    public async Task CreateAsmHackCommand_Executed()
    {
        AsmHackCreationDialogViewModel hackCreationModel = new(Log);
        AsmHackCreationDialog hackCreationDialog = new() { DataContext = hackCreationModel };
        (string name, string description, HackFileContainer[] hackFiles) =
            await hackCreationDialog.ShowDialog<(string, string, HackFileContainer[])>(Window);
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description) || hackFiles is null || hackFiles.Length == 0)
        {
            return;
        }

        AsmHack asmHack = new()
        {
            Name = name,
            Description = description,
            Files = hackFiles.Select(h => new HackFile()
            {
                File = h.HackFileName,
                Destination = h.Destination,
                Parameters = h.Parameters.Select(p => new HackParameter()
                {
                    Name = p.Name,
                    DescriptiveName = p.DescriptiveName,
                    Values = p.Values.Select(v => new HackParameterValue()
                    {
                        Name = v.Name,
                        Value = v.Value,
                    }).ToArray(),
                }).ToArray(),
                Symbols = h.Symbols.Select(s => $"{s.Symbol} = 0x{s.LocationString}").ToArray(),
            }).ToList(),
            InjectionSites = [.. hackFiles.SelectMany(f => f.InjectionSites)],
        };

        string hackSaveFile = (await Window.ShowSaveFilePickerAsync(Strings.Export_Hack,
            [new(Strings.Serial_Loops_ASM_Hack) { Patterns = ["*.slhack"] }],
            $"{asmHack.Name}.slhack")).TryGetLocalPath();
        if (!string.IsNullOrEmpty(hackSaveFile))
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            await File.WriteAllTextAsync(Path.Combine(tempDir, "hack.json"), JsonSerializer.Serialize(asmHack));
            foreach (HackFileContainer hackFile in hackFiles)
            {
                File.Copy(hackFile.HackFilePath, Path.Combine(tempDir, hackFile.HackFileName));
            }

            using FileStream fs = File.Create(hackSaveFile);
            ZipFile.CreateFromDirectory(tempDir, fs);

            await Window.ShowMessageBoxAsync(Strings.Hack_Created_Successfully_, Strings.The_hack_file_has_been_successfully_created__To_import_it__open_the_ASM_hacks_dialog_and_select__Import_Hack__,
                ButtonEnum.Ok, Icon.Success, Log);
        }
    }

    private async Task EditUiTextCommand_Executed()
    {
        EditUiTextDialogViewModel editUiTextDialogViewModel = new(OpenProject, Log);
        EditUiTextDialog editUiTextDialog = new() { DataContext = editUiTextDialogViewModel };
        await editUiTextDialog.ShowDialog(Window);
    }

    private async Task EditTutorialMappingsCommand_Executed()
    {
        EditTutorialMappingsDialogViewModel viewModel = new(OpenProject, EditorTabs, Log);
        EditTutorialMappingsDialog dialog = new() { DataContext = viewModel };
        await dialog.ShowDialog(Window);
    }

    private async Task ProjectSettingsCommand_Executed()
    {
        ProjectSettingsDialogViewModel projectSettingsDialogViewModel = new();
        ProjectSettingsDialog projectSettingsDialog = new();
        projectSettingsDialogViewModel.Initialize(projectSettingsDialog, OpenProject.Settings, Log);
        projectSettingsDialog.DataContext = projectSettingsDialogViewModel;
        await projectSettingsDialog.ShowDialog(Window);
    }

    private async Task ExportProjectCommand_Executed()
    {
        string exportPath = (await Window.ShowSaveFilePickerAsync(Strings.Export_Project,
            [new(Strings.Exported_Project) { Patterns = ["*.slzip"] }], $"{OpenProject.Name}.slzip"))?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(exportPath))
        {
            OpenProject.Export(exportPath, Log);
        }
    }

    private async Task ExportPatchCommand_Executed()
    {
        ExportPatchDialogViewModel exportPatchDialogVm = new(OpenProject, Log);
        await new ExportPatchDialog { DataContext = exportPatchDialogVm }.ShowDialog(Window);
    }

    private async Task CloseProjectView()
    {
        if (await CloseProject_Executed(null))
        {
            return;
        }

        Title = BASE_TITLE;
        OpenHomePanel();

        OpenProject = null;
        EditorTabs = null;
        ItemExplorer = null;
        ToolBar.Items.Clear();

        NativeMenu menu = NativeMenu.GetMenu(Window);
        if (WindowMenu.ContainsKey(MenuHeader.PROJECT))
        {
            menu.Items.Remove(WindowMenu[MenuHeader.PROJECT]);
            WindowMenu.Remove(MenuHeader.PROJECT);
        }
        if (WindowMenu.ContainsKey(MenuHeader.TOOLS))
        {
            menu.Items.Remove(WindowMenu[MenuHeader.TOOLS]);
            WindowMenu.Remove(MenuHeader.TOOLS);
        }
        if (WindowMenu.ContainsKey(MenuHeader.BUILD))
        {
            menu.Items.Remove(WindowMenu[MenuHeader.BUILD]);
            WindowMenu.Remove(MenuHeader.BUILD);
        }
        ProjectsCache.HadProjectOpenOnLastClose = false;
        UpdateRecentProjects();
    }

    private void UpdateRecentProjects()
    {
        RecentProjectsMenu.Menu = [];

        List<string> projectsToRemove = [];
        foreach (string project in ProjectsCache.RecentProjects)
        {
            NativeMenuItem recentProject = new()
            {
                Header = Path.GetFileNameWithoutExtension(project),
                ToolTip = project,
                Command = OpenRecentProjectCommand,
                CommandParameter = project,
            };
            if (!File.Exists(project))
            {
                if (CurrentConfig.RemoveMissingProjects)
                {
                    projectsToRemove.Add(project);
                    continue;
                }

                recentProject.IsEnabled = false;
                recentProject.Header += Strings.Missing_;
                recentProject.Icon = ControlGenerator.GetIcon("Warning", Log);
            }

            RecentProjectsMenu.Menu.Items.Add(recentProject);
        }

        RecentProjectsMenu.IsEnabled = RecentProjectsMenu.Menu.Items.Count > 0;

        projectsToRemove.ForEach(project =>
        {
            ProjectsCache.RecentProjects.Remove(project);
            ProjectsCache.RecentWorkspaces.Remove(project);
        });
        ProjectsCache.Save(Log);
    }

    public async Task AboutCommand_Executed()
    {
        AboutDialogViewModel aboutDialogViewModel = new();
        AboutDialog aboutDialog = new()
        {
            DataContext = aboutDialogViewModel,
        };
        await aboutDialog.ShowDialog(Window);
    }

    public async Task NewProjectCommand_Executed()
    {
        ProjectCreationDialogViewModel projectCreationDialogViewModel = new(CurrentConfig, this, Log);
        Project newProject = await new ProjectCreationDialog
        {
            DataContext = projectCreationDialogViewModel,
        }.ShowDialog<Project>(Window);
        if (newProject is not null)
        {
            OpenProject = newProject;
            OpenProjectView(OpenProject, new ProgressDialogViewModel(Strings.Open_Project));
        }
    }

    public async Task OpenProjectCommand_Executed()
    {
        IStorageFile projectFile = await Window.ShowOpenFilePickerAsync(Strings.Open_Project, [new(Strings.Serial_Loops_Project) { Patterns = [$"*.{Project.PROJECT_FORMAT}"] }], CurrentConfig.ProjectsDirectory);
        if (projectFile is not null)
        {
            await OpenProjectFromPath(projectFile.Path.LocalPath);
        }
    }

    public async Task OpenRecentProjectCommand_Executed(string project)
    {
        await OpenProjectFromPath(project);
    }

    public async Task OpenProjectFromPath(string path)
    {
        Project.LoadProjectResult result = new(Project.LoadProjectState.FAILED); // start us off with a failure
        string projectFileName = Path.GetFileName(path);
        ProgressDialogViewModel tracker = new(string.Format(Strings.Loading_Project____0__, projectFileName));
        tracker.InitializeTasks(() => (OpenProject, result) = Project.OpenProject(path, CurrentConfig, Strings.ResourceManager.GetString, Log, tracker),
            () => { });
        await new ProgressDialog { DataContext = tracker }.ShowDialog(Window);
        if (OpenProject is not null && result.State == Project.LoadProjectState.LOOSELEAF_FILES)
        {
            if (await Window.ShowMessageBoxAsync(
                    Strings.Build_Unbuilt_Files_,
                    Strings.Saved_but_unbuilt_files_were_detected_in_the_project_directory__Would_you_like_to_build_before_loading_the_project__Not_building_could_result_in_these_files_being_overwritten_,
                    ButtonEnum.YesNo, Icon.Question, Log) == ButtonResult.Yes)
            {
                ProgressDialogViewModel secondTracker = new(string.Format(Strings.Loading_Project____0__, projectFileName));
                secondTracker.InitializeTasks(() => Build.BuildIterative(OpenProject, CurrentConfig, Log, secondTracker),
                    () => { });
                await new ProgressDialog { DataContext = secondTracker }.ShowDialog(Window);
            }

            ProgressDialogViewModel thirdTracker = new(string.Format(Strings.Loading_Project____0__, projectFileName));
            thirdTracker.InitializeTasks(() => OpenProject.LoadArchives(Log, thirdTracker),
                () => { });
            await new ProgressDialog { DataContext = thirdTracker }.ShowDialog(Window);
        }
        else if (result.State == Project.LoadProjectState.CORRUPTED_FILE)
        {
            if ((await Window.ShowMessageBoxAsync(Strings.Corrupted_File_Detected_,
                    string.Format(Strings.While_attempting_to_build___file___0_X3__in_archive__1__was_found_to_be_corrupt__Serial_Loops_can_delete_this_file_from_your_base_directory_automatically_which_may_allow_you_to_load_the_rest_of_the_project__but_any_changes_made_to_that_file_will_be_lost__Alternatively__you_can_attempt_to_edit_the_file_manually_to_fix_it__How_would_you_like_to_proceed__Press_OK_to_proceed_with_deleting_the_file_and_Cancel_to_attempt_to_deal_with_it_manually_,
                        result.BadFileIndex, result.BadArchive),
                    ButtonEnum.OkCancel, Icon.Warning, Log)) == ButtonResult.Ok)
            {
                switch (result.BadArchive)
                {
                    case "dat.bin":
                        File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "data",
                            $"{result.BadFileIndex:X3}.s"));
                        break;

                    case "grp.bin":
                        File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "graphics",
                            $"{result.BadFileIndex:X3}.png"));
                        File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "graphics",
                            $"{result.BadFileIndex:X3}.gi"));
                        break;

                    case "evt.bin":
                        File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "events",
                            $"{result.BadFileIndex:X3}.s"));
                        break;
                }

                await OpenProjectFromPath(path);
                return;
            }
            else
            {
                OpenProject = null;
            }
        }

        if (OpenProject is not null)
        {
            OpenProjectView(OpenProject, tracker);
        }
        else
        {
            await CloseProjectView();
        }
    }

    public async Task ImportProjectCommand_Executed(string slzipPath)
    {
        (slzipPath, string romPath) = await new ImportProjectDialog()
        {
            DataContext = new ImportProjectDialogViewModel(slzipPath, Log),
        }.ShowDialog<(string, string)>(Window);
        if (!string.IsNullOrEmpty(slzipPath) && !string.IsNullOrEmpty(romPath))
        {
            if (await CloseProject_Executed(null))
            {
                return;
            }

            Project.LoadProjectResult result = new() { State = Project.LoadProjectState.FAILED };
            ProgressDialogViewModel tracker = new(Strings.Importing_Project);
            tracker.InitializeTasks(() => (OpenProject, result) = Project.Import(slzipPath, romPath, CurrentConfig,
                    Strings.ResourceManager.GetString, Log, tracker),
                () => { });
            await new ProgressDialog { DataContext = tracker }.ShowDialog(Window);

            if (OpenProject is not null)
            {
                OpenProjectView(OpenProject, tracker);
            }
            else
            {
                await CloseProjectView();
            }
        }
    }

    public async Task PreferencesCommand_Executed()
    {
        PreferencesDialogViewModel preferencesDialogViewModel = new();
        PreferencesDialog preferencesDialog = new();
        preferencesDialogViewModel.Initialize(preferencesDialog, Log);
        preferencesDialog.DataContext = preferencesDialogViewModel;
        await preferencesDialog.ShowDialog(Window);
        if (preferencesDialogViewModel.Saved)
        {
            CurrentConfig = preferencesDialogViewModel.Configuration;
            if (preferencesDialogViewModel.RequireRestart)
            {
                if ((await Window.ShowMessageBoxAsync(Strings.Restart_required, Strings.The_changes_made_will_require_Serial_Loops_to_be_restarted__Is_that_okay_, ButtonEnum.YesNo, Icon.Setting, Log)) == ButtonResult.Yes)
                {
                    Window.RestartOnClose = true;
                    Window.Close();
                    return;
                }
                await PreferencesCommand_Executed();
            }
        }
    }

    public async Task EditSaveFileCommand_Executed()
    {
        IStorageFile saveFile = await Window.ShowOpenFilePickerAsync(Strings.Open_Chokuretsu_Save_File,
            [new(Strings.Chokuretsu_Save_File) { Patterns = ["*.sav"] }]);
        if (saveFile is null)
        {
            return;
        }
        string savePath = saveFile.TryGetLocalPath();

        if (OpenProject is not null)
        {
            SaveItem saveItem = new(savePath, Path.GetFileNameWithoutExtension(savePath));
            OpenProject.Items.Add(saveItem);
            EditorTabs.OpenTab(saveItem);
        }
        else
        {
            string rom = Path.Combine(Path.GetDirectoryName(savePath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(savePath)}.nds");
            if (!File.Exists(rom))
            {
                IStorageFile romFile = await Window.ShowOpenFilePickerAsync(Strings.Open_ROM,
                    [new(Strings.NDS_ROM) { Patterns = ["*.nds"] }]);
                if (romFile is null)
                {
                    return;
                }
                rom = romFile.TryGetLocalPath();
            }

            string projectName = $"{Path.GetFileNameWithoutExtension(savePath)}_Temp";
            string tempProjectDirectory = Path.Combine(CurrentConfig.ProjectsDirectory, projectName);
            if (Directory.Exists(tempProjectDirectory))
            {
                if (await Window.ShowMessageBoxAsync(Strings.Temporary_Project_Already_Exists_,
                        string.Format(
                            Strings.In_order_to_edit_this_save_file__Serial_Loops_needs_to_make_a_temporary_project__However__a_project_called___0___already_exists__Would_you_like_to_overwrite_this_project_,
                            projectName),
                        ButtonEnum.YesNo, Icon.Warning, Log) == ButtonResult.Yes)
                {
                    Directory.Delete(tempProjectDirectory, true);
                }
                else
                {
                    return;
                }
            }
            OpenProject = new(projectName, "en", CurrentConfig, Strings.ResourceManager.GetString, Log);
            ProgressDialogViewModel tracker = new(Strings.Creating_Project);
            tracker.InitializeTasks(() =>
            {
                ((IProgressTracker)tracker).Focus(Strings.Creating_Project, 1);
                IO.OpenRom(OpenProject, rom, Log, tracker);
                tracker.Finished++;
                OpenProject.Load(CurrentConfig, Log, tracker);
            }, () =>
            {
                SaveItem saveItem = new(savePath, Path.GetFileNameWithoutExtension(savePath));
                OpenProject.Items.Add(saveItem);
                Window.MainContent.Content = new SaveEditorView
                {
                    DataContext =
                        new SaveEditorViewModel(saveItem, this, Log, null),
                };
                if (WindowMenu.ContainsKey(MenuHeader.TOOLS))
                {
                    // Skip adding the new menu items if they're already here
                    return;
                }

                // Add a few commands to the menu
                NativeMenu menu = NativeMenu.GetMenu(Window);
                int insertionPoint = menu.Items.Count;
                if (((NativeMenuItem)menu.Items.Last()).Header.Equals(Strings._Help))
                {
                    insertionPoint--;
                }

                // PROJECT
                WindowMenu.Add(MenuHeader.PROJECT, new(Strings._Project));
                WindowMenu[MenuHeader.PROJECT].Menu =
                [
                    new NativeMenuItem
                    {
                        Header = Strings.Save_Save_File,
                        Command = SaveProjectCommand,
                        Icon = ControlGenerator.GetIcon("Save", Log),
                        Gesture = SaveHotKey,
                    },
                    new NativeMenuItem
                    {
                        Header = Strings.Close_Save_File,
                        Command = CloseProjectCommand,
                        Icon = ControlGenerator.GetIcon("Close", Log),
                        Gesture = CloseProjectKey,
                    },
                ];
                menu.Items.Insert(insertionPoint, WindowMenu[MenuHeader.PROJECT]);
            });
            await new ProgressDialog { DataContext = tracker }.ShowDialog(Window);
        }
    }

    public void SaveProject_Executed()
    {
        if (OpenProject is null)
        {
            return;
        }

        IEnumerable<ItemDescription> unsavedItems = OpenProject.Items.Where(i => i.UnsavedChanges);
        bool savedEventTable = false;
        bool savedChrData = false;
        bool savedExtra = false;
        bool savedMessInfo = false;
        bool changedScenario = false;
        bool changedNameplates = false;
        bool changedTopics = false;
        bool changedSubs = false;
        List<int> changedLayouts = [];
        SKCanvas nameplateCanvas = new(OpenProject.NameplateBitmap);
        SKCanvas speakerCanvas = new(OpenProject.SpeakerBitmap);

        Dictionary<string, IncludeEntry[]> includes = new()
        {
            {
                "GRPBIN",
                OpenProject.Grp.GetSourceInclude().Split('\n').Where(s => !string.IsNullOrEmpty(s))
                    .Select(i => new IncludeEntry(i)).ToArray()
            },
            {
                "DATBIN",
                OpenProject.Dat.GetSourceInclude().Split('\n').Where(s => !string.IsNullOrEmpty(s))
                    .Select(i => new IncludeEntry(i)).ToArray()
            },
            {
                "EVTBIN",
                OpenProject.Evt.GetSourceInclude().Split('\n').Where(s => !string.IsNullOrEmpty(s))
                    .Select(i => new IncludeEntry(i)).ToArray()
            }
        };

        foreach (ItemDescription item in unsavedItems)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    if (!savedExtra)
                    {
                        IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.Extra.Index:X3}.s"),
                            OpenProject.Extra.GetSource([]), OpenProject, Log);
                        savedExtra = true;
                    }

                    ((BackgroundItem)item).Write(OpenProject, Log);
                    break;
                case ItemDescription.ItemType.BGM:
                    if (!savedExtra)
                    {
                        IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.Extra.Index:X3}.s"),
                            OpenProject.Extra.GetSource([]), OpenProject, Log);
                        savedExtra = true;
                    }
                    break;
                case ItemDescription.ItemType.Character:
                    CharacterItem characterItem = (CharacterItem)item;
                    if (characterItem.NameplateProperties.Name != item.DisplayName[4..])
                    {
                        characterItem.Rename($"CHR_{characterItem.NameplateProperties.Name}", OpenProject);
                        nameplateCanvas.DrawBitmap(
                            characterItem.GetNewNameplate(_blankNameplate, _blankNameplateBaseArrow, OpenProject),
                            new SKRect(0, 16 * ((int)characterItem.MessageInfo.Character - 1), 64,
                                16 * ((int)characterItem.MessageInfo.Character)));
                        speakerCanvas.DrawBitmap(
                            characterItem.GetNewNameplate(_blankNameplate, _blankNameplateBaseArrow, OpenProject,
                                transparent: true),
                            new SKRect(0, 16 * ((int)characterItem.MessageInfo.Character - 1), 64,
                                16 * ((int)characterItem.MessageInfo.Character)));
                        changedNameplates = true;
                    }

                    if (!savedMessInfo)
                    {
                        IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.MessInfo.Index:X3}.s"),
                            OpenProject.MessInfo.GetSource([]), OpenProject, Log);
                        savedMessInfo = true;
                    }
                    break;
                case ItemDescription.ItemType.Character_Sprite:
                    if (!savedChrData)
                    {
                        IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.ChrData.Index:X3}.s"),
                            OpenProject.ChrData.GetSource(new()
                            {
                                { "GRPBIN", OpenProject.Grp.GetSourceInclude().Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => new IncludeEntry(l)).ToArray() }
                            }), OpenProject, Log);
                        savedChrData = true;
                    }
                    CharacterSpriteItem characterSpriteItem = (CharacterSpriteItem)item;
                    characterSpriteItem.Graphics.Write(OpenProject, Log);
                    break;
                case ItemDescription.ItemType.Chess_Puzzle:
                    ChessPuzzleItem chessPuzzleItem = (ChessPuzzleItem)item;
                    IO.WriteStringFile(Path.Combine("assets", "data", $"{chessPuzzleItem.ChessPuzzle.Index:X3}.s"),
                        chessPuzzleItem.ChessPuzzle.GetSource([]), OpenProject, Log);
                    break;
                case ItemDescription.ItemType.Group_Selection:
                    GroupSelectionItem groupSelectionItem = (GroupSelectionItem)item;
                    OpenProject.Scenario.Selects[groupSelectionItem.Index] = groupSelectionItem.Selection;
                    changedScenario = true;
                    break;
                case ItemDescription.ItemType.Place:
                    PlaceItem placeItem = (PlaceItem)item;
                    if (placeItem.PlaceName != item.DisplayName[4..])
                    {
                        placeItem.Rename($"PLC_{placeItem.PlaceName}", OpenProject);
                    }

                    MemoryStream placeStream = new();
                    SKBitmap newPlaceImage =
                        PlaceItem.Unscramble(PlaceItem.Unscramble(placeItem.GetNewPlaceGraphic(_msGothicHaruhi)));
                    placeItem.PlaceGraphic.SetImage(newPlaceImage);
                    newPlaceImage.Encode(placeStream, SKEncodedImageFormat.Png, 1);
                    IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{placeItem.PlaceGraphic.Index:X3}.png"),
                        placeStream.ToArray(), OpenProject, Log);
                    IO.WriteStringFile(Path.Combine("assets", "graphics", $"{placeItem.PlaceGraphic.Index:X3}.gi"),
                        placeItem.PlaceGraphic.GetGraphicInfoFile(), OpenProject, Log);
                    break;
                case ItemDescription.ItemType.Item:
                    ((ItemItem)item).Write(OpenProject, Log);
                    break;
                case ItemDescription.ItemType.Layout:
                    GraphicsFile layout = ((LayoutItem)item).Layout;
                    if (!changedLayouts.Contains(layout.Index))
                    {
                        changedLayouts.Add(layout.Index);
                        IO.WriteStringFile(Path.Combine("assets", "graphics", $"{layout.Index:X3}.lay"), JsonSerializer.Serialize(layout.LayoutEntries, Project.SERIALIZER_OPTIONS), OpenProject, Log);
                    }
                    break;
                case ItemDescription.ItemType.Puzzle:
                    PuzzleFile puzzle = ((PuzzleItem)item).Puzzle;
                    IO.WriteStringFile(Path.Combine("assets", "data", $"{puzzle.Index:X3}.s"), puzzle.GetSource(includes), OpenProject, Log);
                    break;
                case ItemDescription.ItemType.Scenario:
                    ScenarioStruct scenario = ((ScenarioItem)item).Scenario;
                    OpenProject.Scenario.Commands = scenario.Commands;
                    changedScenario = true;
                    break;
                case ItemDescription.ItemType.Script:
                    if (!savedEventTable)
                    {
                        OpenProject.RecalculateEventTable();
                        IO.WriteStringFile(Path.Combine("assets", "events", $"{OpenProject.EventTableFile.Index:X3}.s"), OpenProject.EventTableFile.GetSource(includes), OpenProject, Log);
                    }
                    EventFile evt = ((ScriptItem)item).Event;
                    evt.CollectGarbage();
                    IO.WriteStringFile(Path.Combine("assets", "events", $"{evt.Index:X3}.s"), evt.GetSource(includes), OpenProject, Log);
                    break;
                case ItemDescription.ItemType.System_Texture:
                    ((SystemTextureItem)item).Write(OpenProject, Log);
                    break;
                case ItemDescription.ItemType.Topic:
                    changedTopics = true;
                    break;
                case ItemDescription.ItemType.Voice:
                    VoicedLineItem vce = (VoicedLineItem)item;
                    if (OpenProject.VoiceMap is not null)
                    {
                        changedSubs = true;
                    }
                    break;
                case ItemDescription.ItemType.Save:
                    SaveItem save = (SaveItem)item;
                    try
                    {
                        File.WriteAllBytes(save.SaveLoc, save.Save.GetBytes());
                    }
                    catch (Exception ex)
                    {
                        Log.LogException(Strings.Failed_to_save_Chokuretsu_save_file_, ex);
                    }
                    break;
                default:
                    Log.LogWarning($"Saving for {item.Type}s not yet implemented.");
                    break;
            }

            item.UnsavedChanges = false;
        }

        if (changedScenario)
        {
            IO.WriteStringFile(
                Path.Combine("assets", "events", $"{OpenProject.Evt.GetFileByName("SCENARIOS").Index:X3}.s"),
                OpenProject.Scenario.GetSource(includes, Log), OpenProject, Log);
        }

        if (changedNameplates)
        {
            nameplateCanvas.Flush();
            speakerCanvas.Flush();
            MemoryStream nameplateStream = new();
            OpenProject.NameplateBitmap.Encode(nameplateStream, SKEncodedImageFormat.Png, 1);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", "B87.png"), nameplateStream.ToArray(),
                OpenProject, Log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", "B87.gi"), JsonSerializer.Serialize(OpenProject.NameplateInfo),
                OpenProject, Log);
        }

        if (changedTopics)
        {
            IO.WriteStringFile(Path.Combine("assets", "events", $"{OpenProject.TopicFile.Index:X3}.s"),
                OpenProject.TopicFile.GetSource([]), OpenProject, Log);
        }

        if (changedSubs)
        {
            IO.WriteStringFile(Path.Combine("assets", "events", $"{OpenProject.VoiceMap.Index:X3}.s"),
                OpenProject.VoiceMap.GetSource(), OpenProject, Log);
        }
    }

    public void SearchProject_Executed()
    {
        if (OpenProject == null)
        {
            return;
        }
        SearchDialog searchDialog = new() { DataContext = new SearchDialogViewModel(OpenProject, EditorTabs, Log) };
        searchDialog.Show(Window);
    }

    public async Task BuildIterative_Executed()
    {
        if (OpenProject is not null)
        {
            bool buildSucceeded = true; // imo it's better to have a false negative than a false positive here
            ProgressDialogViewModel tracker = new(Strings.Building_Iteratively, Strings.Building_);
            tracker.InitializeTasks(() => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, Log, tracker), async () =>
            {
                if (buildSucceeded)
                {
                    Log.Log("Build succeeded!");
                    await Window.ShowMessageBoxAsync(Strings.Build_Result, Strings.Build_succeeded_, ButtonEnum.Ok, Icon.Success, Log);
                }
                else
                {
                    Log.LogError(Strings.Build_failed_);
                }
            });
            await new ProgressDialog { DataContext = tracker }.ShowDialog(Window);
        }
    }

    public async Task BuildBase_Executed()
    {
        if (OpenProject is not null)
        {
            bool buildSucceeded = true;
            ProgressDialogViewModel tracker = new(Strings.Building_from_Scratch, Strings.Building_);
            tracker.InitializeTasks(() => buildSucceeded = Build.BuildBase(OpenProject, CurrentConfig, Log, tracker), async () =>
            {
                if (buildSucceeded)
                {
                    Log.Log("Build succeeded!");
                    await Window.ShowMessageBoxAsync(Strings.Build_Result, Strings.Build_succeeded_, ButtonEnum.Ok, Icon.Success, Log);
                }
                else
                {
                    Log.LogError(Strings.Build_failed_);
                }
            });
            await new ProgressDialog { DataContext = tracker } .ShowDialog(Window);
        }
    }

    public async Task BuildAndRun_Executed()
    {
        if (OpenProject is not null)
        {
            if (string.IsNullOrWhiteSpace(CurrentConfig.EmulatorPath) && string.IsNullOrWhiteSpace(CurrentConfig.EmulatorFlatpak))
            {
                Log.LogWarning("Attempted to build and run project while no emulator path/flatpak was set.");
                await Window.ShowMessageBoxAsync(Strings.No_Emulator_Path, Strings.No_emulator_path_has_been_set__nPlease_set_the_path_to_a_Nintendo_DS_emulator_in_Preferences_to_use_Build___Run_,
                    ButtonEnum.Ok, Icon.Warning, Log);
                await PreferencesCommand_Executed();
                return;
            }
            bool buildSucceeded = true;
            ProgressDialogViewModel tracker = new(Strings.Building_and_Running, Strings.Building_);
            tracker.InitializeTasks(() => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, Log, tracker), () =>
                {
                    if (buildSucceeded)
                    {
                        Log.Log("Build succeeded!");
                        try
                        {
                            // If the EmulatorPath is an .app bundle, we need to run the executable inside it
                            string emulatorExecutable = CurrentConfig.EmulatorPath;
                            if (!string.IsNullOrWhiteSpace(CurrentConfig.EmulatorFlatpak))
                            {
                                emulatorExecutable = "flatpak";
                            }
                            if (emulatorExecutable.EndsWith(".app"))
                            {
                                emulatorExecutable = Path.Combine(CurrentConfig.EmulatorPath, "Contents", "MacOS",
                                    Path.GetFileNameWithoutExtension(CurrentConfig.EmulatorPath));
                            }

                            string[] emulatorArgs = [Path.Combine(OpenProject.MainDirectory, $"{OpenProject.Name}.nds")];
                            if (emulatorExecutable.Equals("flatpak"))
                            {
                                emulatorArgs =
                                [
                                    "run", CurrentConfig.EmulatorFlatpak,
                                    Path.Combine(OpenProject.MainDirectory, $"{OpenProject.Name}.nds")
                                ];
                            }
                            Process.Start(emulatorExecutable, emulatorArgs);
                        }
                        catch (Exception ex)
                        {
                            Log.LogException($"Failed to start emulator", ex);
                        }
                    }
                    else
                    {
                        Log.LogError(Strings.Build_failed_);
                    }
                });
            await new ProgressDialog { DataContext = tracker }.ShowDialog(Window);
        }
    }

    private void InitializeProjectMenu()
    {
        if (WindowMenu.ContainsKey(MenuHeader.TOOLS))
        {
            // Skip adding the new menu items if they're already here
            return;
        }
        NativeMenu menu = NativeMenu.GetMenu(Window);
        int insertionPoint = menu.Items.Count;
        if (((NativeMenuItem)menu.Items.Last()).Header.Equals(Strings._Help))
        {
            insertionPoint--;
        }

        // PROJECT
        WindowMenu.Add(MenuHeader.PROJECT, new(Strings._Project));
        WindowMenu[MenuHeader.PROJECT].Menu =
        [
            new NativeMenuItem
            {
                Header = Strings.Save_Project,
                Command = SaveProjectCommand,
                Icon = ControlGenerator.GetIcon("Save", Log),
                Gesture = SaveHotKey,
            },
            new NativeMenuItem
            {
                Header = Strings.Project_Settings___,
                Command = ProjectSettingsCommand,
                Icon = ControlGenerator.GetIcon("Project_Options", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Migrate_to_new_ROM,
                Command = MigrateProjectCommand,
                Icon = ControlGenerator.GetIcon("Migrate_ROM", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Export_Project,
                Command = ExportProjectCommand,
                // Icon = ControlGenerator.GetIcon("Export_Project", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Export_Patch,
                Command = ExportPatchCommand,
                Icon = ControlGenerator.GetIcon("Export_Patch", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Close_Project,
                Command = CloseProjectCommand,
                Icon = ControlGenerator.GetIcon("Close", Log),
                Gesture = CloseProjectKey,
            },
        ];
        menu.Items.Insert(insertionPoint, WindowMenu[MenuHeader.PROJECT]);
        insertionPoint++;

        // TOOLS
        WindowMenu.Add(MenuHeader.TOOLS, new(Strings._Tools));
        WindowMenu[MenuHeader.TOOLS].Menu =
        [
            new NativeMenuItem
            {
                Header = Strings.Apply_Hacks___,
                Command = ApplyHacksCommand,
                Icon = ControlGenerator.GetIcon("Apply_Hacks", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Create_ASM_Hack,
                Command = CreateAsmHackCommand,
            },
            new NativeMenuItem
            {
                Header = Strings.Edit_UI_Text___,
                Command = EditUiTextCommand,
                Icon = ControlGenerator.GetIcon("Edit_UI_Text", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Edit_Tutorial_Mappings___,
                Command = EditTutorialMappingsCommand,
                Icon = ControlGenerator.GetIcon("Tutorial", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Search___,
                Command = SearchProjectCommand,
                Icon = ControlGenerator.GetIcon("Search", Log),
                Gesture = SearchHotKey,
            }
        ];
        menu.Items.Insert(insertionPoint, WindowMenu[MenuHeader.TOOLS]);
        insertionPoint++;

        // BUILD
        WindowMenu.Add(MenuHeader.BUILD, new(Strings._Build));
        WindowMenu[MenuHeader.BUILD].Menu =
        [
            new NativeMenuItem
            {
                Header = Strings.Build,
                Command = BuildIterativeCommand,
                Icon = ControlGenerator.GetIcon("Build", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Build_from_Scratch,
                Command = BuildBaseCommand,
                Icon = ControlGenerator.GetIcon("Build_Scratch", Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Build_and_Run,
                Command = BuildAndRunCommand,
                Icon = ControlGenerator.GetIcon("Build_Run", Log),
            },
        ];
        menu.Items.Insert(insertionPoint, WindowMenu[MenuHeader.BUILD]);

        NativeMenu.SetMenu(Window, menu);

        ToolBar.Items.Clear();
        ToolBar.Items.Add(new ToolbarButton
        {
            Text = Strings.Save,
            Command = SaveProjectCommand,
            Icon = ControlGenerator.GetVectorIcon("Save", Log),
        });
        ToolBar.Items.Add(new ToolbarButton
        {
            Text = Strings.Build,
            Command = BuildIterativeCommand,
            Icon = ControlGenerator.GetVectorIcon("Build", Log),
        });
        ToolBar.Items.Add(new ToolbarButton
        {
            Text = Strings.Build_and_Run,
            Command = BuildAndRunCommand,
            Icon = ControlGenerator.GetVectorIcon("Build_Run", Log),
        });
        ToolBar.Items.Add(new ToolbarButton
        {
            Text = Strings.Search,
            Command = SearchProjectCommand,
            Icon = ControlGenerator.GetVectorIcon("Search", Log),
        });
    }
}
