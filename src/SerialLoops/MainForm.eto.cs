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
        private SubMenuItem _recentProjects;
        private LoopyLogger _log;

        void InitializeComponent()
        {
            Title = BASE_TITLE;
            ClientSize = new(1000, 600);
            MinimumSize = new(769, 420);
            Padding = 10;
            Closing += CloseProject_Executed;

            InitializeBaseMenu();
        }

        private void OpenProjectView(Project project, IProgressTracker tracker)
        {
            EditorTabs = new(project, _log);
            ItemExplorer = new(project, EditorTabs, _log);
            Title = $"{BASE_TITLE} - {project.Name}";
            Content = new TableLayout(new TableRow(ItemExplorer, EditorTabs));

            InitializeProjectMenu();
            LoadCachedData(project, tracker);
        }

        private void CloseProjectView()
        {
            CancelEventArgs cancelEvent = new();
            CloseProject_Executed(this, cancelEvent);
            if (cancelEvent.Cancel) { return; }

            Title = BASE_TITLE;
            Content = new HomePanel(this, _log);

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
            Command newProject = new() { MenuText = "New Project...", ToolBarText = "New Project", Image = ControlGenerator.GetIcon("New", _log) };
            newProject.Executed += NewProjectCommand_Executed;

            Command openProject = new() { MenuText = "Open Project...", ToolBarText = "Open Project", Image = ControlGenerator.GetIcon("Open", _log) };
            openProject.Executed += OpenProject_Executed;

            Command exportPatch = new() { MenuText = "Export Patch", ToolBarText = "Export Patch" };
            exportPatch.Executed += Patch_Executed;

            // Application Items
            Command preferencesCommand = new();
            preferencesCommand.Executed += PreferencesCommand_Executed;

            // About
            Command aboutCommand = new() { MenuText = "About...", Image = ControlGenerator.GetIcon("Help", _log) };
            AboutDialog aboutDialog = new() { ProgramName = "Serial Loops", Developers = new[] { "Jonko", "William" }, Copyright = "© Haroohie Translation Club, 2023", Website = new Uri("https://haroohie.club") };
            aboutCommand.Executed += (sender, e) => aboutDialog.ShowDialog(this);

            // Create Menu
            _recentProjects = new() { Text = "Recent Projects" };
            Menu = new MenuBar
            {
                Items =
                {
                    // File submenu
                    new SubMenuItem { Text = "&File", Items = { newProject, openProject, _recentProjects, exportPatch } }
                },
                ApplicationItems =
                {
                    // application (OS X) or file menu (others)
                    new ButtonMenuItem { Text = "&Preferences...", Command = preferencesCommand, Image = ControlGenerator.GetIcon("Options", _log) },
                },
                AboutItem = aboutCommand
            };
        }

        private void InitializeProjectMenu()
        {
            // File
            Command saveProject = new() { MenuText = "Save Project", ToolBarText = "Save Project", Shortcut = Application.Instance.CommonModifier | Keys.S, Image = ControlGenerator.GetIcon("Save", _log) };
            saveProject.Executed += SaveProject_Executed;

            Command projectSettings = new() { MenuText = "Project Settings...", ToolBarText = "Project Settings", Image = ControlGenerator.GetIcon("Project_Options", _log) };
            projectSettings.Executed += ProjectSettings_Executed;

            Command closeProject = new() { MenuText = "Close Project", ToolBarText = "Close Project", Image = ControlGenerator.GetIcon("Close", _log) };
            closeProject.Executed += (sender, args) => CloseProjectView();

            // Tools
            Command searchProject = new() { MenuText = "Search...", ToolBarText = "Search", Shortcut = Application.Instance.CommonModifier | Keys.F, Image = ControlGenerator.GetIcon("Search", _log) };
            searchProject.Executed += Search_Executed;

            Command findOrphanedItems = new() { MenuText = "Find Orphaned Items..." };
            findOrphanedItems.Executed += FindOrphanedItems_Executed;

            // Build
            Command buildIterativeProject = new() { MenuText = "Build", ToolBarText = "Build", Image = ControlGenerator.GetIcon("Build", _log) };
            buildIterativeProject.Executed += BuildIterativeProject_Executed;

            Command buildBaseProject = new() { MenuText = "Build from Scratch", ToolBarText = "Build from Scratch", Image = ControlGenerator.GetIcon("Build_Scratch", _log) };
            buildBaseProject.Executed += BuildBaseProject_Executed;

            Command buildAndRunProject = new() { MenuText = "Build and Run", ToolBarText = "Run", Image = ControlGenerator.GetIcon("Build_Run", _log) };
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

            // Add project items to menu
            if (Menu.Items[0] is SubMenuItem fileMenu)
            {
                fileMenu.Items.Add(saveProject);
                fileMenu.Items.Add(projectSettings);
                fileMenu.Items.Add(closeProject);
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
                ProjectsCache.Save(_log);
                UpdateRecentProjects();

                if (CurrentConfig.RememberProjectWorkspace)
                {
                    tracker.Focus("Restoring Workspace", openTabs.Count);
                    foreach (string itemName in openTabs)
                    {
                        ItemDescription item = project.FindItem(itemName);
                        if (item is not null)
                        {
                            Application.Instance.Invoke(() => EditorTabs.OpenTab(item, _log));
                        }
                        tracker.Finished++;
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogError($"Failed to load cached data: {e.Message}");
                string projectFile = project.ProjectFile;
                ProjectsCache.RecentWorkspaces.Remove(projectFile);
                ProjectsCache.RecentProjects.Remove(projectFile);
                ProjectsCache.Save(_log);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _log = new();
            CurrentConfig = Config.LoadConfig(_log);
            _log.Initialize(CurrentConfig);            
            ProjectsCache = ProjectsCache.LoadCache(CurrentConfig, _log);
            UpdateRecentProjects();

            if (CurrentConfig.AutoReopenLastProject && ProjectsCache.RecentProjects.Count > 0)
            {
                OpenProjectFromPath(ProjectsCache.RecentProjects[0]);
            } else
            {
                Content = new HomePanel(this, _log);
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
                    recentProject.Image = ControlGenerator.GetIcon("Warning", _log);
                }
                _recentProjects?.Items.Add(recentProject);
            }
            if (_recentProjects is not null) { _recentProjects.Enabled = _recentProjects.Items.Count > 0; }

            projectsToRemove.ForEach(project =>
            {
                ProjectsCache.RecentProjects.Remove(project);
                ProjectsCache.RecentWorkspaces.Remove(project);
            });
            ProjectsCache.Save(_log);
        }

        public void NewProjectCommand_Executed(object sender, EventArgs e)
        {
            ProjectCreationDialog projectCreationDialog = new() { Config = CurrentConfig, Log = _log };
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
            LoopyProgressTracker tracker = new();
            _ = new ProgressDialog(() => OpenProject = Project.OpenProject(path, CurrentConfig, _log, tracker), () => 
            {
                if (OpenProject is not null)
                {
                    OpenProjectView(OpenProject, tracker);
                }
            }, tracker, "Loading Project");
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
                    case ItemDescription.ItemType.BGM:
                        if (!savedExtra)
                        {
                            IO.WriteStringFile(Path.Combine("assets", "data", $"{OpenProject.Extra.Index:X3}.s"), OpenProject.Extra.GetSource(new()), OpenProject, _log);
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
                            }, _log),
                            OpenProject, _log);
                        break;
                    case ItemDescription.ItemType.Script:
                        EventFile evt = ((ScriptItem)item).Event;
                        evt.CollectGarbage();
                        IO.WriteStringFile(Path.Combine("assets", "events", $"{evt.Index:X3}.s"), evt.GetSource(new()), OpenProject, _log);
                        break;

                    default:
                        _log.LogWarning($"Saving for {item.Type}s not yet implemented.");
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
                ProjectSettingsDialog projectSettingsDialog = new(OpenProject, _log);
                projectSettingsDialog.ShowModal(this);
            }
        }

        private void Search_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                SearchDialog searchDialog = new(_log)
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
                        orphanedItemsDialog = new(OpenProject, ItemExplorer, EditorTabs, _log);
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
                ProgressDialog loadingDialog = new(() => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, _log, tracker), () =>
                {
                    if (buildSucceeded)
                    {
                        _log.Log("Build succeeded!");
                        MessageBox.Show("Build succeeded!", "Build Result", MessageBoxType.Information);
                    }
                    else
                    {
                        _log.LogError("Build failed!");
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
                ProgressDialog loadingDialog = new(() => buildSucceeded = Build.BuildBase(OpenProject, CurrentConfig, _log, tracker), () =>
                {
                    if (buildSucceeded)
                    {
                        _log.Log("Build succeeded!");
                        MessageBox.Show("Build succeeded!", "Build Result", MessageBoxType.Information);
                    }
                    else
                    {
                        _log.LogError("Build failed!");
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
                    _log.LogWarning("No emulator path set. Please set the path to your emulator.");
                    return;
                }

                bool buildSucceeded = true;
                LoopyProgressTracker tracker = new("Building:");
                ProgressDialog loadingDialog = new(() => buildSucceeded = Build.BuildIterative(OpenProject, CurrentConfig, _log, tracker), () =>
                {
                    if (buildSucceeded)
                    {
                        _log.Log("Build succeeded!");
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
                            _log.LogError($"Failed to start emulator: {ex.Message}");
                        }
                    }
                    else
                    {
                        _log.LogError("Build failed!");
                    }
                }, tracker, "Building and Running");
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
            PreferencesDialog preferencesDialog = new(CurrentConfig, _log);
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
                ProjectsCache.Save(_log);
            }
        }
    }
}
