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
        public ItemExplorerPanel ItemExplorer { get; set; }
        public LoopyLogger Log { get; set; }
        private SubMenuItem _recentProjects;

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
            InitializeProjectMenu();
            
            EditorTabs = new(project, this, Log);
            ItemExplorer = new(project, EditorTabs, Log);
            Title = $"{BASE_TITLE} - {project.Name}";
            Content = new TableLayout(new TableRow(ItemExplorer, EditorTabs));
            EditorTabs.Tabs_PageChanged(this, EventArgs.Empty);

            LoadCachedData(project, tracker);
        }

        private void CloseProjectView()
        {
            CancelEventArgs cancelEvent = new();
            CloseProject_Executed(this, cancelEvent);
            if (cancelEvent.Cancel) { return; }

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
            Command newProject = new() { MenuText = "New Project...", ToolBarText = "New Project", Image = ControlGenerator.GetIcon("New", Log) };
            newProject.Executed += NewProjectCommand_Executed;

            Command openProject = new() { MenuText = "Open Project...", ToolBarText = "Open Project", Image = ControlGenerator.GetIcon("Open", Log) };
            openProject.Executed += OpenProject_Executed;

            // Application Items
            Command preferencesCommand = new();
            preferencesCommand.Executed += PreferencesCommand_Executed;

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
                Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
            }.ShowDialog(this);

            // Create Menu
            _recentProjects = new() { Text = "Recent Projects" };
            Menu = new MenuBar
            {
                Items =
                {
                    // File submenu
                    new SubMenuItem { Text = "&File", Items = { newProject, openProject, _recentProjects } }
                },
                ApplicationItems =
                {
                    // application (OS X) or file menu (others)
                    new ButtonMenuItem { Text = "&Preferences...", Command = preferencesCommand, Image = ControlGenerator.GetIcon("Options", Log) },
                    new ButtonMenuItem { Text = "&Check For Updates...", Command = checkForUpdatesCommand, Image = ControlGenerator.GetIcon("Update", Log) }
                },
                AboutItem = aboutCommand
            };
        }

        private void InitializeProjectMenu()
        {
            // File
            Command saveProject = new() { MenuText = "Save Project", ToolBarText = "Save Project", Shortcut = Application.Instance.CommonModifier | Keys.S, Image = ControlGenerator.GetIcon("Save", Log) };
            saveProject.Executed += SaveProject_Executed;

            Command projectSettings = new() { MenuText = "Project Settings...", ToolBarText = "Project Settings", Image = ControlGenerator.GetIcon("Project_Options", Log) };
            projectSettings.Executed += ProjectSettings_Executed;

            Command migrateProject = new() { MenuText = "Migrate to new ROM", ToolBarText = "Migrate Project" };
            migrateProject.Executed += MigrateProject_Executed;

            Command exportPatch = new() { MenuText = "Export Patch", ToolBarText = "Export Patch" };
            exportPatch.Executed += Patch_Executed;

            Command closeProject = new() { MenuText = "Close Project", ToolBarText = "Close Project", Image = ControlGenerator.GetIcon("Close", Log) };
            closeProject.Executed += (sender, args) => CloseProjectView();

            // Tools
            Command searchProject = new() { MenuText = "Search...", ToolBarText = "Search", Shortcut = Application.Instance.CommonModifier | Keys.F, Image = ControlGenerator.GetIcon("Search", Log) };
            searchProject.Executed += Search_Executed;

            Command findOrphanedItems = new() { MenuText = "Find Orphaned Items..." };
            findOrphanedItems.Executed += FindOrphanedItems_Executed;

            // Build
            Command buildIterativeProject = new() { MenuText = "Build", ToolBarText = "Build", Image = ControlGenerator.GetIcon("Build", Log) };
            buildIterativeProject.Executed += BuildIterativeProject_Executed;

            Command buildBaseProject = new() { MenuText = "Build from Scratch", ToolBarText = "Build from Scratch", Image = ControlGenerator.GetIcon("Build_Scratch", Log) };
            buildBaseProject.Executed += BuildBaseProject_Executed;

            Command buildAndRunProject = new() { MenuText = "Build and Run", ToolBarText = "Run", Image = ControlGenerator.GetIcon("Build_Run", Log) };
            buildAndRunProject.Executed += BuildAndRunProject_Executed;

            // Add toolbar
            ToolBar = new ToolBar
            {
                Style = "sl-toolbar",
                Items =
                {
                    ControlGenerator.GetToolBarItem(buildIterativeProject),
                    ControlGenerator.GetToolBarItem(buildAndRunProject),
                    ControlGenerator.GetToolBarItem(searchProject)
                }
            };

            // Add project items to existing File menu
            if (Menu.Items.FirstOrDefault(x => x.Text == "&File") is SubMenuItem fileMenu)
            {
                fileMenu.Items.AddRange(new[] { saveProject, projectSettings, migrateProject, exportPatch, closeProject });
            }
            Menu.Items.Add(new SubMenuItem { Text = "&Tools", Items = { searchProject, findOrphanedItems } });
            Menu.Items.Add(new SubMenuItem { Text = "&Build", Items = { buildIterativeProject, buildBaseProject, buildAndRunProject } });
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
            } else
            {
                Content = new HomePanel(this, Log);
            }
        }

        private void UpdateRecentProjects()
        {
            _recentProjects?.Items.Clear();

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
                _recentProjects?.Items.Add(recentProject);
            }
            if (_recentProjects is not null) { _recentProjects.Enabled = _recentProjects.Items.Count > 0; }

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
            _ = new ProgressDialog(() => (OpenProject, result) = Project.OpenProject(path, CurrentConfig, Log, tracker), () => { }, tracker, "Loading Project");
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

                _ = new ProgressDialog(() => OpenProject.LoadArchives(Log, tracker), () => { }, tracker, "Loading Project");
            }
            else if (result.State == Project.LoadProjectState.CORRUPTED_FILE)
            {
                if (MessageBox.Show($"While attempting to build,  file #{result.BadFileIndex:X3} in archive {result.BadArchive} was " +
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
                            File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "data", $"{result.BadFileIndex:X3}.s"));
                            break;

                        case "grp.bin":
                            File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "graphics", $"{result.BadFileIndex:X3}.png"));
                            File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "graphics", $"{result.BadFileIndex:X3}_pal.csv"));
                            break;

                        case "evt.bin":
                            File.Delete(Path.Combine(OpenProject.BaseDirectory, "assets", "events", $"{result.BadFileIndex:X3}.s"));
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
            foreach (ItemDescription item in unsavedItems)
            {
                switch (item.Type)
                {
                    case ItemDescription.ItemType.Background:
                        if (!savedExtra)
                        {
                            IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.Extra.Index:X3}.s"), OpenProject.Extra.GetSource(new()), OpenProject, Log);
                            savedExtra = true;
                        }
                        ((BackgroundItem)item).Write(OpenProject, Log);
                        break;
                    case ItemDescription.ItemType.BGM:
                        if (!savedExtra)
                        {
                            IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.Extra.Index:X3}.s"), OpenProject.Extra.GetSource(new()), OpenProject, Log);
                            savedExtra = true;
                        }
                        break;
                    case ItemDescription.ItemType.Scenario:
                        ScenarioStruct scenario = ((ScenarioItem)item).Scenario;
                        IO.WriteStringFile(Path.Combine("assets", "events", $"{OpenProject.Evt.Files.First(f => f.Name == "SCENARIOS").Index:X3}.s"),
                            // TODO: Refactor this logic into the chokuretsu library so that we don't end up with ugliness like this for all of our includes
                            scenario.GetSource(new()
                            {
                                { "DATBIN", OpenProject.Dat.GetSourceInclude().Split('\n').Where(s => !string.IsNullOrEmpty(s)).Select(i => new IncludeEntry(i)).ToArray() },
                                { "EVTBIN", OpenProject.Evt.GetSourceInclude().Split('\n').Where(s => !string.IsNullOrEmpty(s)).Select(i => new IncludeEntry(i)).ToArray() }
                            }, Log),
                            OpenProject, Log);
                        break;
                    case ItemDescription.ItemType.Script:
                        EventFile evt = ((ScriptItem)item).Event;
                        evt.CollectGarbage();
                        IO.WriteStringFile(Path.Combine("assets", "events", $"{evt.Index:X3}.s"), evt.GetSource(new()), OpenProject, Log);
                        break;

                    default:
                        Log.LogWarning($"Saving for {item.Type}s not yet implemented.");
                        break;
                }
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

        private void Search_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                SearchDialog searchDialog = new(Log)
                {
                    Project = OpenProject,
                    Tabs = EditorTabs,
                };
                searchDialog.ShowModal(this);
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
                }, () => {
                    Application.Instance.Invoke(() =>
                    {
                        orphanedItemsDialog?.ShowModal(this);
                    });
                }, tracker, "Finding orphaned items");
            }
        }

        private void BuildIterativeProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                bool buildSucceeded = true; // imo it's better to have a false negative than a false positive here
                LoopyProgressTracker tracker = new("Building:");
                ProgressDialog loadingDialog = new(() => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, Log, tracker), () =>
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
                ProgressDialog loadingDialog = new(() => buildSucceeded = Build.BuildBase(OpenProject, CurrentConfig, Log, tracker), () =>
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
                if (CurrentConfig.EmulatorPath is null)
                {
                    MessageBox.Show("No emulator path set. Please set the path to your emulator.", "No Emulator Path", MessageBoxType.Warning);
                    Log.LogWarning("No emulator path set. Please set the path to your emulator.");
                    return;
                }

                bool buildSucceeded = true;
                LoopyProgressTracker tracker = new("Building:");
                ProgressDialog loadingDialog = new(() => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, Log, tracker), () =>
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
                                emulatorExecutable = Path.Combine(CurrentConfig.EmulatorPath, "Contents", "MacOS", Path.GetFileNameWithoutExtension(CurrentConfig.EmulatorPath));
                            }

                            Process.Start(emulatorExecutable, $"\"{Path.Combine(OpenProject.MainDirectory, $"{OpenProject.Name}.nds")}\"");
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
                    _ = new ProgressDialog(() => Patch.CreatePatch(baseRomDialog.FileName, currentRom, outputPatchDialog.FileName),
                        () => MessageBox.Show("Patch Created!", "Success!", MessageBoxType.Information), tracker, "Creating Patch");
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
                    DialogResult result = MessageBox.Show($"You have unsaved changes in {unsavedItems.Count()} item(s). Would you like to save before quitting?", MessageBoxButtons.YesNoCancel, MessageBoxType.Warning);
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
