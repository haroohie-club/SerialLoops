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
                PlaceholderText = "Search...",
                ToolTip = "Search for items by name, ID, or type.",
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
                MenuText = "New Project...",
                ToolBarText = "New Project",
                Image = ControlGenerator.GetIcon("New", Log)
            };
            newProjectCommand.Executed += NewProjectCommand_Executed;

            Command openProjectCommand = new()
            {
                MenuText = "Open Project...",
                ToolBarText = "Open Project",
                Image = ControlGenerator.GetIcon("Open", Log)
            };
            openProjectCommand.Executed += OpenProject_Executed;

            // Application Items
            Command preferencesCommand = new();
            preferencesCommand.Executed += PreferencesCommand_Executed;

            Command viewLogsCommand = new()
            {
                MenuText = "View Logs",
                ToolBarText = "View Logs",
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
            Command aboutCommand = new() { MenuText = "About...", Image = ControlGenerator.GetIcon("Help", Log) };
            aboutCommand.Executed += (sender, e) => new AboutDialog
            {
                ProgramName = "Serial Loops",
                Developers = new[] { "Jonko", "William278" },
                Copyright = "© Haroohie Translation Club, 2023",
                Website = new Uri("https://haroohie.club"),
                Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion,
            }.ShowDialog(this);

            // Create Menu
            _recentProjectsCommand = new() { Text = "Recent Projects" };
            Menu = new MenuBar
            {
                Items =
                {
                    // File submenu
                    new SubMenuItem
                    {
                        Text = "&File",
                        Items =
                        {
                            newProjectCommand,
                            openProjectCommand,
                            _recentProjectsCommand
                        }
                    }
                },
                ApplicationItems =
                {
                    // application (OS X) or file menu (others)
                    new ButtonMenuItem
                    {
                        Text = "&Preferences...", Command = preferencesCommand,
                        Image = ControlGenerator.GetIcon("Options", Log)
                    },
                    new ButtonMenuItem
                    {
                        Text = "&Check For Updates...", Command = checkForUpdatesCommand,
                        Image = ControlGenerator.GetIcon("Update", Log)
                    },
                    new ButtonMenuItem
                    {
                        Text = "View &Logs", Command = viewLogsCommand,
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
                MenuText = "Save Project",
                ToolBarText = "Save",
                Shortcut = Application.Instance.CommonModifier | Keys.S,
                Image = ControlGenerator.GetIcon("Save", Log)
            };
            saveProjectCommand.Executed += SaveProject_Executed;

            Command projectSettingsCommand = new()
            {
                MenuText = "Project Settings...",
                ToolBarText = "Project Settings",
                Image = ControlGenerator.GetIcon("Project_Options", Log),
            };
            projectSettingsCommand.Executed += ProjectSettings_Executed;

            Command migrateProjectCommand = new()
            {
                MenuText = "Migrate to new ROM",
                ToolBarText = "Migrate Project",
                Image = ControlGenerator.GetIcon("Migrate_ROM", Log)
            };
            migrateProjectCommand.Executed += MigrateProject_Executed;

            Command exportPatchCommand = new()
            {
                MenuText = "Export Patch",
                ToolBarText = "Export Patch",
                Image = ControlGenerator.GetIcon("Export_Patch", Log)
            };
            exportPatchCommand.Executed += Patch_Executed;

            Command closeProjectCommand = new()
            {
                MenuText = "Close Project",
                ToolBarText = "Close Project",
                Image = ControlGenerator.GetIcon("Close", Log)
            };
            closeProjectCommand.Executed += (sender, args) => CloseProjectView();

            // Tools
            Command applyHacksCommand = new() { MenuText = "Apply Hacks...", Image = ControlGenerator.GetIcon("Apply_Hacks", Log) };
            applyHacksCommand.Executed += (sender, args) => new RomHacksDialog(OpenProject, CurrentConfig, Log).ShowModal();

            Command renameItemCommand = new() { MenuText = "Rename Item", Shortcut = Keys.F2, Image = ControlGenerator.GetIcon("Rename_Item", Log) };
            renameItemCommand.Executed +=
                (sender, args) => Shared.RenameItem(OpenProject, ItemExplorer, EditorTabs, Log);

            Command editUiTextCommand = new() { MenuText = "Edit UI Text...", Image = ControlGenerator.GetIcon("Edit_UI_Text", Log) };
            editUiTextCommand.Executed += EditUiTextCommand_Executed;

            Command searchProjectCommand = new()
            {
                MenuText = "Search...",
                ToolBarText = "Search",
                Shortcut = Application.Instance.CommonModifier | Keys.F,
                Image = ControlGenerator.GetIcon("Search", Log)
            };
            searchProjectCommand.Executed += Search_Executed;

            Command findOrphanedItemsCommand = new() { MenuText = "Find Orphaned Items...", Image = ControlGenerator.GetIcon("Orphan_Search", Log) };
            findOrphanedItemsCommand.Executed += FindOrphanedItems_Executed;

            // Build
            Command buildIterativeProjectCommand = new()
            {
                MenuText = "Build",
                ToolBarText = "Build",
                Image = ControlGenerator.GetIcon("Build", Log)
            };
            buildIterativeProjectCommand.Executed += BuildIterativeProject_Executed;

            Command buildBaseProjectCommand = new()
            {
                MenuText = "Build from Scratch",
                ToolBarText = "Build from Scratch",
                Image = ControlGenerator.GetIcon("Build_Scratch", Log)
            };
            buildBaseProjectCommand.Executed += BuildBaseProject_Executed;

            Command buildAndRunProjectCommand = new()
            {
                MenuText = "Build and Run",
                ToolBarText = "Run",
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
            if (Menu.Items.FirstOrDefault(x => x.Text.Contains("File")) is SubMenuItem fileMenu)
            {
                fileMenu.Items.AddRange(new[]
                {
                    saveProjectCommand,
                    projectSettingsCommand,
                    migrateProjectCommand,
                    exportPatchCommand,
                    closeProjectCommand
                });
            }

            Menu.Items.Add(new SubMenuItem
            {
                Text = "&Tools",
                Items =
                {
                    applyHacksCommand,
                    renameItemCommand,
                    editUiTextCommand,
                    searchProjectCommand,
                    findOrphanedItemsCommand,
                }
            });
            Menu.Items.Add(new SubMenuItem
            {
                Text = "&Build",
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
                List<string> openTabs = new();
                if (ProjectsCache.RecentWorkspaces.ContainsKey(project.ProjectFile))
                {
                    openTabs = ProjectsCache.RecentWorkspaces[project.ProjectFile];
                }

                ProjectsCache.CacheRecentProject(project.ProjectFile, openTabs);
                ProjectsCache.Save(Log);
                UpdateRecentProjects();

                if (CurrentConfig.RememberProjectWorkspace)
                {
                    tracker.Focus("Restoring Workspace", openTabs.Count);
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
                Log.LogError($"Failed to load cached data: {e.Message}");
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

        private void UpdateRecentProjects()
        {
            _recentProjectsCommand?.Items.Clear();

            List<string> projectsToRemove = new();
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
                    recentProject.MenuText += " (Missing)";
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
                OpenProjectView(OpenProject, new LoopyProgressTracker());
            }
        }

        public void OpenProject_Executed(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Directory = new Uri(CurrentConfig.ProjectsDirectory) };
            openFileDialog.Filters.Add(new("Serial Loops Project", $".{Project.PROJECT_FORMAT}"));
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
            LoopyProgressTracker tracker = new();
            _ = new ProgressDialog(() => (OpenProject, result) = Project.OpenProject(path, CurrentConfig, Log, tracker),
                () => { }, tracker, "Loading Project");
            if (OpenProject is not null && result.State == Project.LoadProjectState.LOOSELEAF_FILES)
            {
                if (MessageBox.Show("Saved but unbuilt files were detected in the project directory. " +
                                    "Would you like to build before loading the project? " +
                                    "Not building could result in these files being overwritten.",
                        "Build Unbuilt Files?",
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
                        $"While attempting to build,  file #{result.BadFileIndex:X3} in archive {result.BadArchive} was " +
                        $"found to be corrupt. Serial Loops can delete this file from your base directory automatically which may allow you to load the rest of the " +
                        $"project, but any changes made to that file will be lost. Alternatively, you can attempt to edit the file manually to fix it. How would " +
                        $"you like to proceed? Press OK to proceed with deleting the file and Cancel to attempt to deal with it manually.",
                        "Corrupted File Detected!",
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
            bool savedExtra = false;
            bool changedNameplates = false;
            bool changedTopics = false;
            bool changedSubs = false;
            SKCanvas nameplateCanvas = new(OpenProject.NameplateBitmap);
            SKCanvas speakerCanvas = new(OpenProject.SpeakerBitmap);

            foreach (ItemDescription item in unsavedItems)
            {
                switch (item.Type)
                {
                    case ItemDescription.ItemType.Background:
                        if (!savedExtra)
                        {
                            IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.Extra.Index:X3}.s"),
                                OpenProject.Extra.GetSource(new()), OpenProject, Log);
                            savedExtra = true;
                        }

                        ((BackgroundItem)item).Write(OpenProject, Log);
                        break;
                    case ItemDescription.ItemType.Character:
                        CharacterItem characterItem = (CharacterItem)item;
                        if (characterItem.NameplateProperties.Name != item.DisplayName[4..])
                        {
                            Shared.RenameItem(OpenProject, ItemExplorer, EditorTabs, Log,
                                $"CHR_{characterItem.NameplateProperties.Name}");
                        }

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
                        break;
                    case ItemDescription.ItemType.BGM:
                        if (!savedExtra)
                        {
                            IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.Extra.Index:X3}.s"),
                                OpenProject.Extra.GetSource(new()), OpenProject, Log);
                            savedExtra = true;
                        }

                        break;
                    case ItemDescription.ItemType.Place:
                        PlaceItem placeItem = (PlaceItem)item;
                        if (placeItem.PlaceName != item.DisplayName[4..])
                        {
                            Shared.RenameItem(OpenProject, ItemExplorer, EditorTabs, Log, $"PLC_{placeItem.PlaceName}");
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
                            Path.Combine("assets", "events",
                                $"{OpenProject.Evt.Files.First(f => f.Name == "SCENARIOS").Index:X3}.s"),
                            // TODO: Refactor this logic into the chokuretsu library so that we don't end up with ugliness like this for all of our includes
                            scenario.GetSource(new()
                            {
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
                            }, Log),
                            OpenProject, Log);
                        break;
                    case ItemDescription.ItemType.Script:
                        EventFile evt = ((ScriptItem)item).Event;
                        evt.CollectGarbage();
                        IO.WriteStringFile(Path.Combine("assets", "events", $"{evt.Index:X3}.s"), evt.GetSource(new()),
                            OpenProject, Log);
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
                    OpenProject.TopicFile.GetSource(new()), OpenProject, Log);
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
                LoopyProgressTracker tracker = new("");
                ((IProgressTracker)tracker).Focus("Finding orphaned items...", 1);

                ProgressDialog _ = new(() =>
                    {
                        Application.Instance.Invoke(() =>
                        {
                            orphanedItemsDialog = new(OpenProject, ItemExplorer, EditorTabs, Log);
                            tracker.Finished++;
                        });
                    }, () => { Application.Instance.Invoke(() => { orphanedItemsDialog?.Show(); }); }, tracker,
                    "Finding orphaned items");
            }
        }

        private void BuildIterativeProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                bool buildSucceeded = true; // imo it's better to have a false negative than a false positive here
                LoopyProgressTracker tracker = new("Building:");
                ProgressDialog loadingDialog = new(
                    () => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, Log, tracker), () =>
                    {
                        if (buildSucceeded)
                        {
                            Log.Log("Build succeeded!");
                            MessageBox.Show("Build succeeded!", "Build Result", MessageBoxType.Information);
                        }
                        else
                        {
                            Log.LogError("Build failed!");
                        }
                    }, tracker, "Building Iteratively");
            }
        }

        private void BuildBaseProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                bool buildSucceeded = true;
                LoopyProgressTracker tracker = new("Building:");
                ProgressDialog loadingDialog = new(
                    () => buildSucceeded = Build.BuildBase(OpenProject, CurrentConfig, Log, tracker), () =>
                    {
                        if (buildSucceeded)
                        {
                            Log.Log("Build succeeded!");
                            MessageBox.Show("Build succeeded!", "Build Result", MessageBoxType.Information);
                        }
                        else
                        {
                            Log.LogError("Build failed!");
                        }
                    }, tracker, "Building from Scratch");
            }
        }

        private void BuildAndRunProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                if (string.IsNullOrWhiteSpace(CurrentConfig.EmulatorPath))
                {
                    MessageBox.Show("No emulator path has been set.\nPlease set the path to a Nintendo DS emulator in Preferences to use Build & Run.",
                        "No Emulator Path", MessageBoxType.Warning);
                    Log.LogWarning("Attempted to build and run project while no emulator path was set.");
                    PreferencesCommand_Executed(sender, e);
                    return;
                }

                bool buildSucceeded = true;
                LoopyProgressTracker tracker = new("Building:");
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
                                Log.LogError($"Failed to start emulator: {ex.Message}");
                            }
                        }
                        else
                        {
                            Log.LogError("Build failed!");
                        }
                    }, tracker, "Building and Running");
            }
        }

        private void MigrateProject_Executed(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Title = "New Base ROM" };
            openFileDialog.Filters.Add(new("Chokuretsu ROM", ".nds"));

            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                LoopyProgressTracker tracker = new();
                _ = new ProgressDialog(() =>
                    {
                        OpenProject.MigrateProject(openFileDialog.FileName, Log, tracker);
                        OpenProject.Load(CurrentConfig, Log, tracker);
                    },
                    () => MessageBox.Show("Migrated to new ROM!", "Migration Complete!", MessageBoxType.Information),
                    tracker, "Migrating to new ROM");
            }
        }

        private void Patch_Executed(object sender, EventArgs e)
        {
            OpenFileDialog baseRomDialog = new() { Title = "Select base ROM" };
            baseRomDialog.Filters.Add(new() { Name = "NDS ROM", Extensions = new string[] { ".nds" } });
            if (baseRomDialog.ShowAndReportIfFileSelected(this))
            {
                string currentRom = Path.Combine(OpenProject.MainDirectory, $"{OpenProject.Name}.nds");

                SaveFileDialog outputPatchDialog = new() { Title = "Output patch location" };
                outputPatchDialog.Filters.Add(new() { Name = "XDelta patch", Extensions = new string[] { ".xdelta" } });
                if (outputPatchDialog.ShowAndReportIfFileSelected(this))
                {
                    LoopyProgressTracker tracker = new();
                    _ = new ProgressDialog(
                        () => Patch.CreatePatch(baseRomDialog.FileName, currentRom, outputPatchDialog.FileName),
                        () => MessageBox.Show("Patch Created!", "Success!", MessageBoxType.Information), tracker,
                        "Creating Patch");
                }
            }
        }

        public void PreferencesCommand_Executed(object sender, EventArgs e)
        {
            PreferencesDialog preferencesDialog = new(CurrentConfig, Log);
            preferencesDialog.ShowModal(this);
            CurrentConfig = preferencesDialog.Configuration;
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
                        $"You have unsaved changes in {unsavedItems.Count()} item(s)." +
                        $"Would you like to save before closing the project?",
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