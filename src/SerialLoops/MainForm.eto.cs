using Eto.Forms;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SerialLoops
{
    public partial class MainForm : Form
    {
        private const string BASE_TITLE = "Serial Loops";

        public ProjectsCache ProjectsCache { get; set; }
        public Config CurrentConfig { get; set; }
        public Project OpenProject { get; set; }
        public EditorTabsPanel EditorTabs { get; set; }
        public SearchBox SearchBox { get; set; }
        public ItemExplorerPanel ItemExplorer { get; set; }
        public LoopyLogger Log { get; set; }
        private SubMenuItem _recentProjectsCommand;

        private SKBitmap _blankNameplate, _blankNameplateBaseArrow;
        private SKTypeface _msGothicHaruhi;

        public string ShutdownUpdateUrl = null;

        void InitializeComponent()
        {
            Title = BASE_TITLE;
            ClientSize = new(1000, 600);
            MinimumSize = new(769, 420);
            Padding = 10;
            Closing += CloseProject_Executed;
            Closed += AppClosed_Executed;

            InitializeBaseMenu();
        }

        private void OpenProjectView(Project project, IProgressTracker tracker)
        {
            InitializeBaseMenu();
            InitializeProjectMenu();

            using Stream blankNameplateStream = Assembly.GetCallingAssembly()
                .GetManifestResourceStream("SerialLoops.Graphics.BlankNameplate.png");
            _blankNameplate = SKBitmap.Decode(blankNameplateStream);
            using Stream blankNameplateBaseArrowStream = Assembly.GetCallingAssembly()
                .GetManifestResourceStream("SerialLoops.Graphics.BlankNameplateBaseArrow.png");
            _blankNameplateBaseArrow = SKBitmap.Decode(blankNameplateBaseArrowStream);
            using Stream typefaceStream = Assembly.GetCallingAssembly()
                .GetManifestResourceStream("SerialLoops.Graphics.MS-Gothic-Haruhi.ttf");
            _msGothicHaruhi = SKTypeface.FromStream(typefaceStream);

            EditorTabs = new(project, this, Log);

            SearchBox = new()
            {
                PlaceholderText = Application.Instance.Localize(this, "Search..."),
                ToolTip = Application.Instance.Localize(this, "Search for items by name, ID, or type."),
                Width = 200,
            };
            Button advancedSearchButton = new() { Text = "...", Width = 25 };
            advancedSearchButton.Click += Search_Executed;
            TableLayout searchBarLayout = new(new TableRow(
                SearchBox,
                new StackLayout { Items = { advancedSearchButton }, Width = 25 }
            ))
            { Spacing = new(5, 0) };

            ItemExplorer = new(project, EditorTabs, SearchBox, Log);
            Title = $"{BASE_TITLE} - {project.Name}";
            Content = new TableLayout(new TableRow
            (
                new TableLayout(searchBarLayout, ItemExplorer) { Spacing = new(0, 5) },
                EditorTabs
            ))
            { Spacing = new(0, 5) };
            EditorTabs.Tabs_PageChanged(this, EventArgs.Empty);

            LoadCachedData(project, tracker);
        }

        private void CloseProjectView()
        {
            CancelEventArgs cancelEvent = new();
            CloseProject_Executed(this, cancelEvent);
            if (cancelEvent.Cancel)
            {
                return;
            }

            Title = BASE_TITLE;
            Content = new HomePanel(this, Log);

            OpenProject = null;
            EditorTabs = null;
            ItemExplorer = null;

            // Setting a toolbar to null on Mac triggers a crash, and clearing the items doesn't hide it on Windows.
            if (Application.Instance.Platform.IsMac)
            {
                ToolBar?.Items.Clear();
            }
            else
            {
                ToolBar = null;
            }

            InitializeBaseMenu();
            ProjectsCache.HadProjectOpenOnLastClose = false;
            UpdateRecentProjects();
        }

        private void InitializeBaseMenu()
        {
            // File
            Command newProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "New Project..."),
                ToolBarText = Application.Instance.Localize(this, "New Project"),
                Image = ControlGenerator.GetIcon("New", Log)
            };
            newProjectCommand.Executed += NewProjectCommand_Executed;

            Command openProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Open Project..."),
                ToolBarText = Application.Instance.Localize(this, "Open Project"),
                Image = ControlGenerator.GetIcon("Open", Log)
            };
            openProjectCommand.Executed += OpenProject_Executed;

            Command editSaveFileCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Edit Save File..."),
                ToolBarText = Application.Instance.Localize(this, "Edit Save File"),
                Image = ControlGenerator.GetIcon("Edit_Save", Log)
            };
            editSaveFileCommand.Executed += EditSaveFileCommand_Executed;

            // Application Items
            Command preferencesCommand = new();
            preferencesCommand.Executed += PreferencesCommand_Executed;

            Command viewLogsCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "View Logs"),
                ToolBarText = Application.Instance.Localize(this, "View Logs"),
            };
            viewLogsCommand.Executed += (sender, args) =>
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
            };

            // Check For Updates
            Command checkForUpdatesCommand = new();
            checkForUpdatesCommand.Executed += (sender, e) => new UpdateChecker(this).Check();

            // About
            Command aboutCommand = new() { MenuText = Application.Instance.Localize(this, "About..."), Image = ControlGenerator.GetIcon("Help", Log) };
            aboutCommand.Executed += AboutCommand_Executed;

            // Create Menu
            _recentProjectsCommand = new() { Text = Application.Instance.Localize(this, "Recent Projects") };
            Menu = new MenuBar
            {
                Items =
                {
                    // File submenu
                    new SubMenuItem
                    {
                        Text = Application.Instance.Localize(this, "&File"),
                        Items =
                        {
                            newProjectCommand,
                            openProjectCommand,
                            _recentProjectsCommand,
                            new SeparatorMenuItem(),
                            editSaveFileCommand
                        }
                    }
                },
                ApplicationItems =
                {
                    // application (OS X) or file menu (others)
                    new ButtonMenuItem
                    {
                        Text = Application.Instance.Localize(this, "&Preferences..."), Command = preferencesCommand,
                        Image = ControlGenerator.GetIcon("Options", Log)
                    },
                    new ButtonMenuItem
                    {
                        Text = Application.Instance.Localize(this, "&Check For Updates..."), Command = checkForUpdatesCommand,
                        Image = ControlGenerator.GetIcon("Update", Log)
                    },
                    new ButtonMenuItem
                    {
                        Text = Application.Instance.Localize(this, "View &Logs"), Command = viewLogsCommand,
                    },
                },
                AboutItem = aboutCommand
            };
        }

        private void InitializeProjectMenu()
        {
            // File
            Command saveProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Save Project"),
                ToolBarText = Application.Instance.Localize(this, "Save"),
                Shortcut = Application.Instance.CommonModifier | Keys.S,
                Image = ControlGenerator.GetIcon("Save", Log)
            };
            saveProjectCommand.Executed += SaveProject_Executed;

            Command projectSettingsCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Project Settings..."),
                ToolBarText = Application.Instance.Localize(this, "Project Settings"),
                Image = ControlGenerator.GetIcon("Project_Options", Log),
            };
            projectSettingsCommand.Executed += ProjectSettings_Executed;

            Command migrateProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Migrate to new ROM"),
                ToolBarText = Application.Instance.Localize(this, "Migrate Project"),
                Image = ControlGenerator.GetIcon("Migrate_ROM", Log)
            };
            migrateProjectCommand.Executed += MigrateProject_Executed;

            Command exportPatchCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Export Patch"),
                ToolBarText = Application.Instance.Localize(this, "Export Patch"),
                Image = ControlGenerator.GetIcon("Export_Patch", Log)
            };
            exportPatchCommand.Executed += Patch_Executed;

            Command closeProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Close Project"),
                ToolBarText = Application.Instance.Localize(this, "Close Project"),
                Image = ControlGenerator.GetIcon("Close", Log)
            };
            closeProjectCommand.Executed += (sender, args) => CloseProjectView();

            // Tools
            Command applyHacksCommand = new() { MenuText = Application.Instance.Localize(this, "Apply Hacks..."), Image = ControlGenerator.GetIcon("Apply_Hacks", Log) };
            applyHacksCommand.Executed += (sender, args) => new AsmHacksDialog(OpenProject, CurrentConfig, Log).ShowModal();

            Command renameItemCommand = new() { MenuText = Application.Instance.Localize(this, "Rename Item"), Shortcut = Keys.F2, Image = ControlGenerator.GetIcon("Rename_Item", Log) };
            renameItemCommand.Executed +=
                (sender, args) => Shared.RenameItem(OpenProject, ItemExplorer, EditorTabs, Log);

            Command editUiTextCommand = new() { MenuText = Application.Instance.Localize(this, "Edit UI Text..."), Image = ControlGenerator.GetIcon("Edit_UI_Text", Log) };
            editUiTextCommand.Executed += EditUiTextCommand_Executed;

            Command editTutorialMappingsCommand = new() { MenuText = Application.Instance.Localize(this, "Edit Tutorial Mappings..."), Image = ControlGenerator.GetIcon("Tutorial", Log) };
            editTutorialMappingsCommand.Executed += EditTutorialMappingsCommand_Executed;

            Command searchProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Search..."),
                ToolBarText = Application.Instance.Localize(this, "Search"),
                Shortcut = Application.Instance.CommonModifier | Keys.F,
                Image = ControlGenerator.GetIcon("Search", Log)
            };
            searchProjectCommand.Executed += Search_Executed;

            Command findOrphanedItemsCommand = new() { MenuText = Application.Instance.Localize(this, "Find Orphaned Items..."), Image = ControlGenerator.GetIcon("Orphan_Search", Log) };
            findOrphanedItemsCommand.Executed += FindOrphanedItems_Executed;

            // Build
            Command buildIterativeProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Build"),
                ToolBarText = Application.Instance.Localize(this, "Build"),
                Image = ControlGenerator.GetIcon("Build", Log)
            };
            buildIterativeProjectCommand.Executed += BuildIterativeProject_Executed;

            Command buildBaseProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Build from Scratch"),
                ToolBarText = Application.Instance.Localize(this, "Build from Scratch"),
                Image = ControlGenerator.GetIcon("Build_Scratch", Log)
            };
            buildBaseProjectCommand.Executed += BuildBaseProject_Executed;

            Command buildAndRunProjectCommand = new()
            {
                MenuText = Application.Instance.Localize(this, "Build and Run"),
                ToolBarText = Application.Instance.Localize(this, "Run"),
                Image = ControlGenerator.GetIcon("Build_Run", Log)
            };
            buildAndRunProjectCommand.Executed += BuildAndRunProject_Executed;

            // Add toolbar
            ToolBar = new ToolBar
            {
                Style = "sl-toolbar",
                Items =
                {
                    ControlGenerator.GetToolBarItem(saveProjectCommand),
                    ControlGenerator.GetToolBarItem(buildIterativeProjectCommand),
                    ControlGenerator.GetToolBarItem(buildAndRunProjectCommand),
                    ControlGenerator.GetToolBarItem(searchProjectCommand)
                }
            };

            // Add project items to existing File menu
            if (Menu.Items.FirstOrDefault(x => x.Text.Contains(Application.Instance.Localize(this, "File"))) is SubMenuItem fileMenu)
            {
                foreach (var command in new[]
                     {
                         saveProjectCommand,
                         projectSettingsCommand,
                         migrateProjectCommand,
                         exportPatchCommand,
                         closeProjectCommand
                     })
                {
                    fileMenu.Items.Insert(3, command);
                }
            }

            Menu.Items.Add(new SubMenuItem
            {
                Text = Application.Instance.Localize(this, "&Tools"),
                Items =
                {
                    applyHacksCommand,
                    renameItemCommand,
                    editUiTextCommand,
                    editTutorialMappingsCommand,
                    searchProjectCommand,
                    findOrphanedItemsCommand,
                }
            });
            Menu.Items.Add(new SubMenuItem
            {
                Text = Application.Instance.Localize(this, "&Build"),
                Items =
                {
                    buildIterativeProjectCommand,
                    buildBaseProjectCommand,
                    buildAndRunProjectCommand
                }
            });
        }

        private void LoadCachedData(Project project, IProgressTracker tracker)
        {
            try
            {
                List<string> openTabs = [];
                if (ProjectsCache.RecentWorkspaces.TryGetValue(project.ProjectFile, out List<string> previousTabs))
                {
                    openTabs = previousTabs;
                }

                ProjectsCache.CacheRecentProject(project.ProjectFile, openTabs);
                ProjectsCache.Save(Log);
                UpdateRecentProjects();

                if (CurrentConfig.RememberProjectWorkspace)
                {
                    tracker.Focus(Application.Instance.Localize(this, "Restoring Workspace"), openTabs.Count);
                    foreach (string itemName in openTabs)
                    {
                        ItemDescription item = project.FindItem(itemName);
                        if (item is not null)
                        {
                            Application.Instance.Invoke(() => EditorTabs.OpenTab(item, Log));
                        }

                        tracker.Finished++;
                    }

                    if (EditorTabs.Tabs.Pages.Count > 0)
                    {
                        EditorTabs.Tabs.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogException(Application.Instance.Localize(this, "Failed to load cached data"), e);
                string projectFile = project.ProjectFile;
                ProjectsCache.RecentWorkspaces.Remove(projectFile);
                ProjectsCache.RecentProjects.Remove(projectFile);
                ProjectsCache.Save(Log);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Log = new();
            CurrentConfig = Config.LoadConfig(Log);
            Application.Instance.LocalizeString += Application_LocalizeString;
            Log.Initialize(CurrentConfig);

            ProjectsCache = ProjectsCache.LoadCache(CurrentConfig, Log);
            UpdateRecentProjects();

            if (CurrentConfig.CheckForUpdates)
            {
                new UpdateChecker(this).Check();
            }

            if (CurrentConfig.AutoReopenLastProject && ProjectsCache.RecentProjects.Count > 0)
            {
                OpenProjectFromPath(ProjectsCache.RecentProjects[0]);
            }
            else
            {
                Content = new HomePanel(this, Log);
            }
        }

        private void Application_LocalizeString(object sender, LocalizeEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text))
            {
                e.LocalizedText = Strings.ResourceManager.GetString(e.Text, CultureInfo.CurrentCulture);
            }
        }

        private void UpdateRecentProjects()
        {
            _recentProjectsCommand?.Items.Clear();

            List<string> projectsToRemove = [];
            foreach (string project in ProjectsCache.RecentProjects)
            {
                Command recentProject = new() { MenuText = Path.GetFileNameWithoutExtension(project), ToolTip = project };
                recentProject.Executed += OpenRecentProject_Executed;
                if (!File.Exists(project))
                {
                    if (CurrentConfig.RemoveMissingProjects)
                    {
                        projectsToRemove.Add(project);
                        continue;
                    }

                    recentProject.Enabled = false;
                    recentProject.MenuText += Application.Instance.Localize(this, "(Missing)");
                    recentProject.Image = ControlGenerator.GetIcon("Warning", Log);
                }

                _recentProjectsCommand?.Items.Add(recentProject);
            }

            if (_recentProjectsCommand is not null)
            {
                _recentProjectsCommand.Enabled = _recentProjectsCommand.Items.Count > 0;
            }

            projectsToRemove.ForEach(project =>
            {
                ProjectsCache.RecentProjects.Remove(project);
                ProjectsCache.RecentWorkspaces.Remove(project);
            });
            ProjectsCache.Save(Log);
        }

        public void AboutCommand_Executed(object sender, EventArgs e)
        {
            new AboutDialog
            {
                ProgramName = "Serial Loops",
                Developers = ["Jonko", "William278"],
                Copyright = "© Haroohie Translation Club, 2023",
                Website = new Uri("https://haroohie.club"),
                Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion,
            }.ShowDialog(this);
        }

        public void NewProjectCommand_Executed(object sender, EventArgs e)
        {
            ProjectCreationDialog projectCreationDialog = new() { Config = CurrentConfig, Log = Log };
            projectCreationDialog.ShowModal(this);
            if (projectCreationDialog.NewProject is not null)
            {
                CancelEventArgs cancelEvent = new();
                CloseProject_Executed(this, cancelEvent);
                if (cancelEvent.Cancel)
                {
                    return;
                }

                OpenProject = projectCreationDialog.NewProject;
                OpenProjectView(OpenProject, new LoopyProgressTracker(s => Application.Instance.Localize(null, s)));
            }
        }

        public void OpenProject_Executed(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Directory = new Uri(CurrentConfig.ProjectsDirectory) };
            openFileDialog.Filters.Add(new(Application.Instance.Localize(this, "Serial Loops Project"), $".{Project.PROJECT_FORMAT}"));
            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                CancelEventArgs cancelEvent = new();
                CloseProject_Executed(this, cancelEvent);
                if (cancelEvent.Cancel)
                {
                    return;
                }

                OpenProjectFromPath(openFileDialog.FileName);
            }
        }

        private void OpenRecentProject_Executed(object sender, EventArgs e)
        {
            OpenProjectFromPath(((Command)sender).ToolTip);
        }

        public void OpenProjectFromPath(string path)
        {
            Project.LoadProjectResult result = new(Project.LoadProjectState.FAILED); // start us off with a failure
            LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s));
            _ = new ProgressDialog(() => (OpenProject, result) = Project.OpenProject(path, CurrentConfig, s => Application.Instance.Localize(null, s), Log, tracker),
                () => { }, tracker, Application.Instance.Localize(this, "Loading Project"));
            if (OpenProject is not null && result.State == Project.LoadProjectState.LOOSELEAF_FILES)
            {
                if (MessageBox.Show(Application.Instance.Localize(this, "Saved but unbuilt files were detected in the project directory. " +
                                    "Would you like to build before loading the project? " +
                                    "Not building could result in these files being overwritten."),
                        Application.Instance.Localize(this, "Build Unbuilt Files?"),
                        MessageBoxButtons.YesNo,
                        MessageBoxType.Question,
                        MessageBoxDefaultButton.Yes) == DialogResult.Yes)
                {
                    _ = new ProgressDialog(() => Build.BuildIterative(OpenProject, CurrentConfig, Log, tracker),
                        () => { }, tracker, "Loading Project");
                }

                _ = new ProgressDialog(() => OpenProject.LoadArchives(Log, tracker), () => { }, tracker,
                    "Loading Project");
            }
            else if (result.State == Project.LoadProjectState.CORRUPTED_FILE)
            {
                if (MessageBox.Show(
                        string.Format(Application.Instance.Localize(this, "While attempting to build,  file #{0:X3} in archive {1} was " +
                        $"found to be corrupt. Serial Loops can delete this file from your base directory automatically which may allow you to load the rest of the " +
                        $"project, but any changes made to that file will be lost. Alternatively, you can attempt to edit the file manually to fix it. How would " +
                        $"you like to proceed? Press OK to proceed with deleting the file and Cancel to attempt to deal with it manually."), result.BadFileIndex, result.BadArchive),
                        Application.Instance.Localize(this, "Corrupted File Detected!"),
                        MessageBoxButtons.OKCancel,
                        MessageBoxType.Warning,
                        MessageBoxDefaultButton.Cancel) == DialogResult.Ok)
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
                                $"{result.BadFileIndex:X3}_pal.csv"));
                            break;

                        case "evt.bin":
                            File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "events",
                                $"{result.BadFileIndex:X3}.s"));
                            break;
                    }

                    OpenProjectFromPath(path);
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
                CloseProjectView();
            }
        }

        private void SaveProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject == null)
            {
                return;
            }

            IEnumerable<ItemDescription> unsavedItems = OpenProject.Items.Where(i => i.UnsavedChanges);
            bool savedEventTable = false;
            bool savedExtra = false;
            bool savedMessInfo = false;
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
                            Shared.RenameItem(characterItem, OpenProject, ItemExplorer, EditorTabs, Log,
                                $"CHR_{characterItem.NameplateProperties.Name}");

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
                    case ItemDescription.ItemType.Chibi:
                        ChibiItem chibiItem = (ChibiItem)item;
                        foreach (string modifiedEntryName in chibiItem.ChibiEntryModifications.Where(e => e.Value).Select(e => e.Key))
                        {
                            chibiItem.ChibiEntries.First(e => e.Name == modifiedEntryName).Chibi.Write(OpenProject, Log);
                            chibiItem.ChibiEntryModifications[modifiedEntryName] = false;
                        }
                        break;
                    case ItemDescription.ItemType.Item:
                        ((ItemItem)item).Write(OpenProject, Log);
                        break;
                    case ItemDescription.ItemType.Layout:
                        int layoutIndex = ((LayoutItem)item).Layout.Index;
                        if (!changedLayouts.Contains(layoutIndex))
                        {
                            changedLayouts.Add(layoutIndex);
                            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{layoutIndex:X3}.lay"), OpenProject.LayoutFiles[layoutIndex].GetBytes(), OpenProject, Log);
                        }
                        break;
                    case ItemDescription.ItemType.Place:
                        PlaceItem placeItem = (PlaceItem)item;
                        if (placeItem.PlaceName != item.DisplayName[4..])
                        {
                            Shared.RenameItem(placeItem, OpenProject, ItemExplorer, EditorTabs, Log, $"PLC_{placeItem.PlaceName}");
                        }

                        MemoryStream placeStream = new();
                        SKBitmap newPlaceImage =
                            PlaceItem.Unscramble(PlaceItem.Unscramble(placeItem.GetNewPlaceGraphic(_msGothicHaruhi)));
                        placeItem.PlaceGraphic.SetImage(newPlaceImage);
                        newPlaceImage.Encode(placeStream, SKEncodedImageFormat.Png, 1);
                        IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{placeItem.PlaceGraphic.Index:X3}.png"),
                            placeStream.ToArray(), OpenProject, Log);
                        break;
                    case ItemDescription.ItemType.Scenario:
                        ScenarioStruct scenario = ((ScenarioItem)item).Scenario;
                        IO.WriteStringFile(
                            Path.Combine("assets", "events", $"{OpenProject.Evt.GetFileByName("SCENARIOS").Index:X3}.s"),
                            scenario.GetSource(includes, Log), OpenProject, Log);
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
                    default:
                        Log.LogWarning($"Saving for {item.Type}s not yet implemented.");
                        break;
                }
            }

            if (changedNameplates)
            {
                nameplateCanvas.Flush();
                speakerCanvas.Flush();
                MemoryStream nameplateStream = new();
                OpenProject.NameplateBitmap.Encode(nameplateStream, SKEncodedImageFormat.Png, 1);
                IO.WriteBinaryFile(Path.Combine("assets", "graphics", "B87.png"), nameplateStream.ToArray(),
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

            foreach (Editor editor in EditorTabs.Tabs.Pages.Cast<Editor>())
            {
                editor.UpdateTabTitle(true);
            }
        }

        private void ProjectSettings_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                ProjectSettingsDialog projectSettingsDialog = new(OpenProject, Log);
                projectSettingsDialog.ShowModal(this);
            }
        }

        private void EditUiTextCommand_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                EditUiTextDialog editUiTextDialog = new(OpenProject, Log);
                editUiTextDialog.ShowModal(this);
            }
        }

        private void EditTutorialMappingsCommand_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                EditTutorialMappingsDialog editTutorialMappingsDialog = new(OpenProject, EditorTabs, Log);
                editTutorialMappingsDialog.Show();
            }
        }

        private void Search_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                SearchDialog searchDialog = new(Log)
                {
                    Project = OpenProject,
                    Tabs = EditorTabs,
                    Text = SearchBox.Text,
                };
                searchDialog.Show();
            }
        }

        private void FindOrphanedItems_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                OrphanedItemsDialog orphanedItemsDialog = null;
                LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s), string.Empty);
                ((IProgressTracker)tracker).Focus(Application.Instance.Localize(this, "Finding orphaned items..."), 1);

                ProgressDialog _ = new(() =>
                {
                    Application.Instance.Invoke(() =>
                    {
                        orphanedItemsDialog = new(OpenProject, ItemExplorer, EditorTabs, Log);
                        tracker.Finished++;
                    });
                }, () => { Application.Instance.Invoke(() => { orphanedItemsDialog?.Show(); }); }, tracker,
                Application.Instance.Localize(this, "Finding orphaned items"));
            }
        }

        public void EditSaveFileCommand_Executed(object sender, EventArgs e)
        {
            var openEditor = () =>
            {
                OpenFileDialog openFileDialog = new() { Title = Application.Instance.Localize(this, "Open Chokuretsu Save File") };
                openFileDialog.Filters.Add(new(Application.Instance.Localize(this, "Chokuretsu Save File"), ["*.sav"]));
                if (openFileDialog.ShowAndReportIfFileSelected(this))
                {
                    SaveEditorDialog saveEditorDialog = new(Log, OpenProject, EditorTabs, openFileDialog.FileName);
                    if (saveEditorDialog.LoadedSuccessfully)
                    {
                        saveEditorDialog.Show();
                    }
                }
            };
            if (OpenProject is not null)
            {
                openEditor.Invoke();
                return;
            }

            // Ask user if they wish to create a project
            if (MessageBox.Show(Application.Instance.Localize(this, "To edit Save Files, you need to have a project open.\n" +
                                "No project is currently open. Would you like to create a new project?"),
                Application.Instance.Localize(this, "No Project Open"), MessageBoxButtons.YesNo, MessageBoxType.Question,
                MessageBoxDefaultButton.Yes) == DialogResult.Yes)
            {
                NewProjectCommand_Executed(sender, e);
                if (OpenProject is not null)
                {
                    openEditor.Invoke();
                }
            }
        }


        private void BuildIterativeProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                bool buildSucceeded = true; // imo it's better to have a false negative than a false positive here
                LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s), "Building:");
                ProgressDialog loadingDialog = new(
                    () => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, Log, tracker), () =>
                    {
                        if (buildSucceeded)
                        {
                            Log.Log("Build succeeded!");
                            MessageBox.Show(Application.Instance.Localize(this, "Build succeeded!"), "Build Result", MessageBoxType.Information);
                        }
                        else
                        {
                            Log.LogError(Application.Instance.Localize(this, "Build failed!"));
                        }
                    }, tracker, Application.Instance.Localize(this, "Building Iteratively"));
            }
        }

        private void BuildBaseProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                bool buildSucceeded = true;
                LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s), "Building:");
                ProgressDialog loadingDialog = new(
                    () => buildSucceeded = Build.BuildBase(OpenProject, CurrentConfig, Log, tracker), () =>
                    {
                        if (buildSucceeded)
                        {
                            Log.Log("Build succeeded!");
                            MessageBox.Show(Application.Instance.Localize(this, "Build succeeded!"), Application.Instance.Localize(this, "Build Result"), MessageBoxType.Information);
                        }
                        else
                        {
                            Log.LogError("Build failed!");
                        }
                    }, tracker, Application.Instance.Localize(this, "Building from Scratch"));
            }
        }

        private void BuildAndRunProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                if (string.IsNullOrWhiteSpace(CurrentConfig.EmulatorPath))
                {
                    MessageBox.Show(Application.Instance.Localize(this, "No emulator path has been set.\nPlease set the path to a Nintendo DS emulator in Preferences to use Build & Run."),
                        Application.Instance.Localize(this, "No Emulator Path"), MessageBoxType.Warning);
                    Log.LogWarning("Attempted to build and run project while no emulator path was set.");
                    PreferencesCommand_Executed(sender, e);
                    return;
                }

                bool buildSucceeded = true;
                LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s), "Building:");
                ProgressDialog loadingDialog = new(
                    () => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, Log, tracker), () =>
                    {
                        if (buildSucceeded)
                        {
                            Log.Log("Build succeeded!");
                            try
                            {
                                // If the EmulatorPath is an .app bundle, we need to run the executable inside it
                                string emulatorExecutable = CurrentConfig.EmulatorPath;
                                if (emulatorExecutable.EndsWith(".app"))
                                {
                                    emulatorExecutable = Path.Combine(CurrentConfig.EmulatorPath, "Contents", "MacOS",
                                        Path.GetFileNameWithoutExtension(CurrentConfig.EmulatorPath));
                                }

                                Process.Start(emulatorExecutable,
                                    $"\"{Path.Combine(OpenProject.MainDirectory, $"{OpenProject.Name}.nds")}\"");
                            }
                            catch (Exception ex)
                            {
                                Log.LogException($"Failed to start emulator", ex);
                            }
                        }
                        else
                        {
                            Log.LogError("Build failed!");
                        }
                    }, tracker, Application.Instance.Localize(this, "Building and Running"));
            }
        }

        private void MigrateProject_Executed(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Title = Application.Instance.Localize(this, "New Base ROM") };
            openFileDialog.Filters.Add(new(Application.Instance.Localize(this, "Chokuretsu ROM"), ".nds"));

            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s));
                _ = new ProgressDialog(() =>
                    {
                        OpenProject.MigrateProject(openFileDialog.FileName, Log, tracker);
                        OpenProject.Load(CurrentConfig, Log, tracker);
                    },
                    () => MessageBox.Show(Application.Instance.Localize(this, "Migrated to new ROM!"), Application.Instance.Localize(this, "Migration Complete!"), MessageBoxType.Information),
                    tracker, Application.Instance.Localize(this, "Migrating to new ROM"));
            }
        }

        private void Patch_Executed(object sender, EventArgs e)
        {
            OpenFileDialog baseRomDialog = new() { Title = Application.Instance.Localize(this, "Select base ROM") };
            baseRomDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "NDS ROM"), Extensions = [".nds"] });
            if (baseRomDialog.ShowAndReportIfFileSelected(this))
            {
                string currentRom = Path.Combine(OpenProject.MainDirectory, $"{OpenProject.Name}.nds");

                SaveFileDialog outputPatchDialog = new() { Title = Application.Instance.Localize(this, "Output patch location") };
                outputPatchDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "XDelta patch"), Extensions = [".xdelta"] });
                if (outputPatchDialog.ShowAndReportIfFileSelected(this))
                {
                    LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s));
                    _ = new ProgressDialog(
                        () => Patch.CreatePatch(baseRomDialog.FileName, currentRom, outputPatchDialog.FileName, Log),
                        () => MessageBox.Show(Application.Instance.Localize(this, "Patch Created!"), Application.Instance.Localize(this, "Success!"), MessageBoxType.Information), tracker,
                        Application.Instance.Localize(this, "Creating Patch"));
                }
            }
        }

        public void PreferencesCommand_Executed(object sender, EventArgs e)
        {
            PreferencesDialog preferencesDialog = new(CurrentConfig, Log);
            preferencesDialog.ShowModal(this);
            CurrentConfig = preferencesDialog.Configuration;
            if (preferencesDialog.RequireRestart)
            {
                if (MessageBox.Show(Application.Instance.Localize(this, "The changes made will require Serial Loops to be restarted. Is that okay?"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Application.Instance.Restart();
                }
            }
        }

        public void CloseProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                // Warn against unsaved items
                IEnumerable<ItemDescription> unsavedItems = OpenProject.Items.Where(i => i.UnsavedChanges);
                if (unsavedItems.Any())
                {
                    // message box with yes no cancel buttons
                    DialogResult result = MessageBox.Show(
                        string.Format(Application.Instance.Localize(this, "You have unsaved changes in {0} item(s). " +
                        "Would you like to save before closing the project?"), unsavedItems.Count()),
                        MessageBoxButtons.YesNoCancel, MessageBoxType.Warning
                    );
                    switch (result)
                    {
                        case DialogResult.Yes:
                            SaveProject_Executed(sender, e);
                            BuildIterativeProject_Executed(sender, e); // make sure we lock in the changes
                            break;
                        case DialogResult.Cancel:
                            ((CancelEventArgs)e).Cancel = true;
                            break;
                    }
                }

                // Record open items
                List<string> openItems = EditorTabs.Tabs.Pages.Cast<Editor>()
                    .Select(e => e.Description)
                    .Select(i => i.Name)
                    .ToList();
                ProjectsCache.CacheRecentProject(OpenProject.ProjectFile, openItems);
                ProjectsCache.HadProjectOpenOnLastClose = true;
                ProjectsCache.Save(Log);
            }
        }

        public void AppClosed_Executed(object sender, EventArgs e)
        {
            if (ShutdownUpdateUrl is not null)
            {
                const string updaterExecutable = "LoopyUpdater.exe";
                if (!File.Exists(updaterExecutable))
                {
                    Log.LogError($"Could not start updater: Missing executable ({updaterExecutable})");
                    return;
                }

                Process.Start($"{updaterExecutable} {ShutdownUpdateUrl}");
            }
        }
    }
}