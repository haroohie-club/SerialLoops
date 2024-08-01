using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using MiniToolbar.Avalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views;
using SerialLoops.Views.Dialogs;
using SerialLoops.Views.Panels;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Svg;

namespace SerialLoops.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const string BASE_TITLE = "Serial Loops";

        public string Title { get; set; } = BASE_TITLE;
        public Size MinSize => new(769, 420);
        public Size ClientSize { get; set; } = new(1200, 800);

        private object _previousContent;

        public MainWindow Window { get; set; }
        public ProjectsCache ProjectsCache { get; set; }
        public Config CurrentConfig { get; set; }
        public Project OpenProject { get; set; }
        public OpenProjectPanel ProjectPanel { get; set; }
        public Dictionary<MenuHeader, NativeMenuItem> WindowMenu { get; set; }
        public Toolbar ToolBar => ProjectPanel.ToolBar;
        public EditorTabsPanelViewModel EditorTabs { get; set; }
        public ItemExplorerPanelViewModel ItemExplorer { get; set; }
        public TextBox SearchBox => ItemExplorer.SearchBox;

        public NativeMenuItem RecentProjectsMenu { get; set; } = new(Strings.Recent_Projects);
        public LoopyLogger Log { get; set; }

        private SKBitmap _blankNameplate, _blankNameplateBaseArrow;
        private SKTypeface _msGothicHaruhi;

        public string ShutdownUpdateUrl { get; set; } = null;

        public ICommand NewProjectCommand { get; private set; }
        public ICommand OpenProjectCommand { get; private set; }
        public ICommand OpenRecentProjectCommand { get; private set; }
        public ICommand EditSaveCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public ICommand PreferencesCommand { get; private set; }
        public ICommand CheckForUpdatesCommand { get; private set; }
        public ICommand ViewLogsCommand { get; private set; }

        public ICommand SaveProjectCommand { get; private set; }
        public ICommand ProjectSettingsCommand { get; private set; }
        public ICommand MigrateProjectCommand { get; private set; }
        public ICommand ExportPatchCommand { get; private set; }
        public ICommand CloseProjectCommand { get; private set; }

        public ICommand ApplyHacksCommand { get; private set; }
        public ICommand RenameItemCommand { get; private set; }
        public ICommand EditUiTextCommand { get; private set; }
        public ICommand EditTutorialMappingsCommand { get; private set; }
        public ICommand SearchProjectCommand { get; private set; }
        public ICommand FindOrphanedItemsCommand { get; private set; }

        public ICommand BuildIterativeCommand { get; private set; }
        public ICommand BuildBaseCommand { get; private set; }
        public ICommand BuildAndRunCommand { get; private set; }

        public async void Initialize(MainWindow window)
        {
            NewProjectCommand = ReactiveCommand.CreateFromTask(NewProjectCommand_Executed);
            OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectCommand_Executed);
            OpenRecentProjectCommand = ReactiveCommand.CreateFromTask<string>(OpenRecentProjectCommand_Executed);
            EditSaveCommand = ReactiveCommand.Create(EditSaveFileCommand_Executed);
            AboutCommand = ReactiveCommand.CreateFromTask(AboutCommand_Executed);
            PreferencesCommand = ReactiveCommand.CreateFromTask(PreferencesCommand_Executed);
            CheckForUpdatesCommand = ReactiveCommand.Create(() => new UpdateChecker(this).Check());

            Window = window;
            Log = new();
            CurrentConfig = Config.LoadConfig((s) => s, Log);
            Strings.Culture = new(CurrentConfig.CurrentCultureName);
            Log.Initialize(CurrentConfig);

            var fontStyle = new Style(x => x.OfType<Window>());
            var font = FontFamily.Parse(string.IsNullOrEmpty(CurrentConfig.DisplayFont) ? Strings.Default_Font : CurrentConfig.DisplayFont);
            fontStyle.Add(new Setter(Avalonia.Controls.Primitives.TemplatedControl.FontFamilyProperty, font));
            Application.Current.Styles.Add(fontStyle);

            ProjectsCache = ProjectsCache.LoadCache(CurrentConfig, Log);
            UpdateRecentProjects();

            if (CurrentConfig.CheckForUpdates)
            {
                new UpdateChecker(this).Check();
            }

            if (CurrentConfig.AutoReopenLastProject && ProjectsCache.RecentProjects.Count > 0)
            {
                await OpenProjectFromPath(ProjectsCache.RecentProjects[0]);
            }
            else
            {
                HomePanelViewModel homePanelViewModel = new() { MainWindow = this };
                HomePanel homePanel = new() { ViewModel = homePanelViewModel, DataContext = homePanelViewModel };
                homePanelViewModel.Initialize(this, homePanel);
                Window.Content = homePanel;
            }

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
                catch (Exception)
                {
                    Log.LogError("Failed to open log file directly. " +
                        $"Logs can be found at {Path.Combine(CurrentConfig.UserDirectory, "Logs", "SerialLoops.log")}");
                }
            });

        }

        private void OpenProjectView(Project project, IProgressTracker tracker)
        {
            ItemExplorer = new();
            EditorTabs = new();
            EditorTabs.Initialize(this, project, Log);
            ItemExplorer.Initialize(OpenProject, EditorTabs, SearchBox, Log);
            ProjectPanel = new()
            {
                DataContext = new OpenProjectPanelViewModel(ItemExplorer, EditorTabs),
            };
            EditorTabs.InitializeTabs(ProjectPanel.EditorTabs);
            ItemExplorer.SetupExplorer(ProjectPanel.ItemExplorer.Viewer);

            InitializeProjectMenu();

            //using Stream blankNameplateStream = Assembly.GetCallingAssembly()
            //    .GetManifestResourceStream("SerialLoops.Graphics.BlankNameplate.png");
            //_blankNameplate = SKBitmap.Decode(blankNameplateStream);
            //using Stream blankNameplateBaseArrowStream = Assembly.GetCallingAssembly()
            //    .GetManifestResourceStream("SerialLoops.Graphics.BlankNameplateBaseArrow.png");
            //_blankNameplateBaseArrow = SKBitmap.Decode(blankNameplateBaseArrowStream);
            //using Stream typefaceStream = Assembly.GetCallingAssembly()
            //    .GetManifestResourceStream("SerialLoops.Graphics.MS-Gothic-Haruhi.ttf");
            //_msGothicHaruhi = SKTypeface.FromStream(typefaceStream);

            //Button advancedSearchButton = new() { Content = "...", Width = 25, Command = SearchProjectCommand };
            //advancedSearchButton.Click += Search_Executed;
            //TableLayout searchBarLayout = new(new TableRow(
            //    SearchBox,
            //    new StackLayout { Items = { advancedSearchButton }, Width = 25 }
            //))
            //{ Spacing = new(5, 0) };


            Title = $"{BASE_TITLE} - {project.Name}";
            //EditorTabs.Tabs_PageChanged(this, EventArgs.Empty);

            //LoadCachedData(project, tracker);

            Window.Content = ProjectPanel;
        }

        public async Task CloseProject_Executed(WindowClosingEventArgs e)
        {
            if (OpenProject is not null)
            {
                // Warn against unsaved items
                IEnumerable<ItemDescription> unsavedItems = OpenProject.Items.Where(i => i.UnsavedChanges);
                if (unsavedItems.Any())
                {
                    ButtonResult result;
                    bool skipBuild = false;
                    if (e.CloseReason == WindowCloseReason.OSShutdown) // if the OS is shutting down, we're going to expedite things
                    {
                        result = ButtonResult.Yes;
                        skipBuild = true;
                    }
                    else
                    {
                        // message box with yes no cancel buttons
                        result = await MessageBoxManager.GetMessageBoxStandard(Strings.Confirm,
                            string.Format(Strings.You_have_unsaved_changes_in__0__item_s___Would_you_like_to_save_before_closing_the_project_, unsavedItems.Count()),
                            ButtonEnum.YesNoCancel, Icon.Warning, WindowStartupLocation.CenterScreen).ShowWindowDialogAsync(Window);
                    }
                    switch (result)
                    {
                        case ButtonResult.Yes:
                            //SaveProject_Executed(sender, e);
                            if (!skipBuild)
                            {
                                //BuildIterativeProject_Executed(sender, e); // make sure we lock in the changes
                            }
                            break;
                        default:
                            e.Cancel = true;
                            break;
                    }
                }

                // Record open items
                //List<string> openItems = EditorTabs.Tabs.Pages.Cast<Editor>()
                //    .Select(e => e.Description)
                //    .Select(i => i.Name)
                //    .ToList();
                //ProjectsCache.CacheRecentProject(OpenProject.ProjectFile, openItems);
                //ProjectsCache.HadProjectOpenOnLastClose = true;
                //ProjectsCache.Save(Log);
            }
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
            Project newProject = await new ProjectCreationDialog()
            {
                DataContext = projectCreationDialogViewModel,
            }.ShowDialog<Project>(Window);
            if (newProject is not null)
            {
                OpenProject = newProject;
                OpenProjectView(OpenProject, new LoopyProgressTracker());
            }
        }

        public async Task OpenProjectCommand_Executed()
        {
            FilePickerOpenOptions options = new()
            {
                SuggestedStartLocation = await Window.StorageProvider.TryGetFolderFromPathAsync(CurrentConfig.ProjectsDirectory),
                FileTypeFilter = [new FilePickerFileType(Strings.Serial_Loops_Project) { Patterns = [$"*.{Project.PROJECT_FORMAT}"] }],
            };
            IStorageFile projectFile = (await Window.StorageProvider.OpenFilePickerAsync(options)).FirstOrDefault();
            if (projectFile is not null)
            {
                await OpenProjectFromPath(projectFile.Path.AbsolutePath);
            }
        }

        public async Task OpenRecentProjectCommand_Executed(string project)
        {
            await OpenProjectFromPath(project);
        }

        public async Task OpenProjectFromPath(string path)
        {
            Project.LoadProjectResult result = new(Project.LoadProjectState.FAILED); // start us off with a failure
            LoopyProgressTracker tracker = new();
            await new ProgressDialog(() => (OpenProject, result) = Project.OpenProject(path, CurrentConfig, s => s, Log, tracker),
                () => { }, tracker, Strings.Loading_Project).ShowDialog(Window);
            if (OpenProject is not null && result.State == Project.LoadProjectState.LOOSELEAF_FILES)
            {
                if ((await MessageBoxManager.GetMessageBoxStandard(Strings.Build_Unbuilt_Files_,
                    Strings.Saved_but_unbuilt_files_were_detected_in_the_project_directory__Would_you_like_to_build_before_loading_the_project__Not_building_could_result_in_these_files_being_overwritten_,
                    ButtonEnum.YesNo, Icon.Question, WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(Window)) == ButtonResult.Yes)
                {
                    LoopyProgressTracker secondTracker = new();
                    await new ProgressDialog(() => Build.BuildIterative(OpenProject, CurrentConfig, Log, secondTracker),
                        () => { }, secondTracker, "Loading Project").ShowDialog(Window);
                }

                LoopyProgressTracker thirdTracker = new();
                await new ProgressDialog(() => OpenProject.LoadArchives(Log, thirdTracker), () => { }, thirdTracker,
                    "Loading Project").ShowDialog(Window);
            }
            else if (result.State == Project.LoadProjectState.CORRUPTED_FILE)
            {
                if ((await MessageBoxManager.GetMessageBoxStandard(Strings.Corrupted_File_Detected_,
                        string.Format(Strings.While_attempting_to_build___file___0_X3__in_archive__1__was_found_to_be_corrupt__Serial_Loops_can_delete_this_file_from_your_base_directory_automatically_which_may_allow_you_to_load_the_rest_of_the_project__but_any_changes_made_to_that_file_will_be_lost__Alternatively__you_can_attempt_to_edit_the_file_manually_to_fix_it__How_would_you_like_to_proceed__Press_OK_to_proceed_with_deleting_the_file_and_Cancel_to_attempt_to_deal_with_it_manually_,
                        result.BadFileIndex, result.BadArchive),
                        ButtonEnum.OkCancel, Icon.Warning, WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(Window)) == ButtonResult.Ok)
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
                //CloseProjectView();
            }
        }

        public async Task PreferencesCommand_Executed()
        {
            PreferencesDialogViewModel preferencesDialogViewModel = new();
            PreferencesDialog preferencesDialog = new();
            preferencesDialogViewModel.Initialize(preferencesDialog, CurrentConfig, Log);
            preferencesDialog.DataContext = preferencesDialogViewModel;
            await preferencesDialog.ShowDialog(Window);
            if (preferencesDialogViewModel.Saved)
            {
                CurrentConfig = preferencesDialogViewModel.Configuration;
                if (preferencesDialogViewModel.RequireRestart)
                {
                    if ((await MessageBoxManager.GetMessageBoxStandard(string.Empty, Strings.The_changes_made_will_require_Serial_Loops_to_be_restarted__Is_that_okay_, ButtonEnum.YesNo).ShowWindowDialogAsync(Window)) == ButtonResult.Yes)
                    {
                        Window.RestartOnClose = true;
                        Window.Close();
                    }
                }
            }
        }

        public void EditSaveFileCommand_Executed()
        {

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

            // FILE
            WindowMenu[MenuHeader.FILE].Menu.Add(new NativeMenuItem()
            {
                Header = Strings.Save_Project,
                Command = SaveProjectCommand,
                Icon = ControlGenerator.GetIcon("Save", Log),
            });
            WindowMenu[MenuHeader.FILE].Menu.Add(new NativeMenuItem()
            {
                Header = Strings.Project_Settings___,
                Command = ProjectSettingsCommand,
                Icon = ControlGenerator.GetIcon("Project_Options", Log),
            });
            WindowMenu[MenuHeader.FILE].Menu.Add(new NativeMenuItem()
            {
                Header = Strings.Migrate_to_new_ROM,
                Command = MigrateProjectCommand,
                Icon = ControlGenerator.GetIcon("Migrate_ROM", Log),
            });
            WindowMenu[MenuHeader.FILE].Menu.Add(new NativeMenuItem()
            {
                Header = Strings.Export_Patch,
                Command = ExportPatchCommand,
                Icon = ControlGenerator.GetIcon("Export_Patch", Log),
            });
            WindowMenu[MenuHeader.FILE].Menu.Add(new NativeMenuItem()
            {
                Header = Strings.Close_Project,
                Command = CloseProjectCommand,
                Icon = ControlGenerator.GetIcon("Close", Log),
            });

            // TOOLS
            WindowMenu.Add(MenuHeader.TOOLS, new(Strings._Tools));
            WindowMenu[MenuHeader.TOOLS].Menu =
            [
                new NativeMenuItem()
                {
                    Header = Strings.Apply_Hacks___,
                    Command = ApplyHacksCommand,
                    Icon = ControlGenerator.GetIcon("Apply_Hacks", Log),
                },
                new NativeMenuItem()
                {
                    Header = Strings.Rename_Item,
                    Command = RenameItemCommand,
                    Icon = ControlGenerator.GetIcon("Rename_Item", Log),
                },
                new NativeMenuItem()
                {
                    Header = Strings.Edit_UI_Text___,
                    Command = EditUiTextCommand,
                    Icon = ControlGenerator.GetIcon("Edit_UI_Text", Log),
                },
                new NativeMenuItem()
                {
                    Header = Strings.Edit_Tutorial_Mappings___,
                    Command = EditTutorialMappingsCommand,
                    Icon = ControlGenerator.GetIcon("Tutorial", Log),
                },
                new NativeMenuItem()
                {
                    Header = Strings.Search___,
                    Command = SearchProjectCommand,
                    Icon = ControlGenerator.GetIcon("Search", Log),
                },
                new NativeMenuItem()
                {
                    Header = Strings.Find_Orphaned_Items___,
                    Command = FindOrphanedItemsCommand,
                    Icon = ControlGenerator.GetIcon("Orphan_Search", Log),
                },
            ];
            menu.Items.Insert(insertionPoint, WindowMenu[MenuHeader.TOOLS]);
            insertionPoint++;

            // BUILD
            WindowMenu.Add(MenuHeader.BUILD, new(Strings._Build));
            WindowMenu[MenuHeader.BUILD].Menu =
            [
                new NativeMenuItem()
                {
                    Header = Strings.Build,
                    Command = BuildIterativeCommand,
                    Icon = ControlGenerator.GetIcon("Build", Log),
                },
                new NativeMenuItem()
                {
                    Header = Strings.Build_from_Scratch,
                    Command = BuildBaseCommand,
                    Icon = ControlGenerator.GetIcon("Build_Scratch", Log),
                },
                new NativeMenuItem()
                {
                    Header = Strings.Build_and_Run,
                    Command = BuildAndRunCommand,
                    Icon = ControlGenerator.GetIcon("Build_Run", Log),
                },
            ];
            menu.Items.Insert(insertionPoint, WindowMenu[MenuHeader.BUILD]);

            NativeMenu.SetMenu(Window, menu);

            ToolBar.Items.Clear();
            ToolBar.Items.Add(new ToolbarButton()
            {
                Text = Strings.Save,
                Command = SaveProjectCommand,
                Icon = ControlGenerator.GetVectorIcon("Save", Log),
            });
            ToolBar.Items.Add(new ToolbarButton()
            {
                Text = Strings.Build,
                Command = BuildIterativeCommand,
                Icon = ControlGenerator.GetVectorIcon("Build", Log),
            });
            ToolBar.Items.Add(new ToolbarButton()
            {
                Text = Strings.Build_and_Run,
                Command = BuildAndRunCommand,
                Icon = ControlGenerator.GetVectorIcon("Build_Run", Log),
            });
            ToolBar.Items.Add(new ToolbarButton()
            {
                Text = Strings.Search,
                Command = SearchProjectCommand,
                Icon = ControlGenerator.GetVectorIcon("Search", Log),
            });
        }
    }
}
