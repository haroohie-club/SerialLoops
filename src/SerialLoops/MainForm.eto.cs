using Eto.Forms;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Controls;
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

        private LoopyLogger _log;
        public ProjectsCache ProjectsCache { get; set; }
        public Config CurrentConfig { get; set; }
        public Project OpenProject { get; set; }
        public EditorTabsPanel EditorTabs { get; set; }
        public ItemExplorerPanel ItemExplorer { get; set; }
        
        private readonly SubMenuItem _recentProjects = new() { Text = "Recent Projects" };

        void InitializeComponent()
        {
            Title = BASE_TITLE;
            ClientSize = new(1000, 600);
            MinimumSize = new(769, 420);
            Padding = 10;
            Closing += MainForm_Closed;

            // Commands
            // File
            Command newProject = new() { MenuText = "New Project", ToolBarText = "New Project" };
            newProject.Executed += NewProjectCommand_Executed;

            Command openProject = new() { MenuText = "Open Project", ToolBarText = "Open Project" };
            openProject.Executed += OpenProject_Executed;

            Command saveProject = new() { MenuText = "Save Project", ToolBarText = "Save Project", Shortcut = Application.Instance.CommonModifier | Keys.S };
            saveProject.Executed += SaveProject_Executed;

            // Tools
            Command searchProject = new() { MenuText = "Search", ToolBarText = "Search", Shortcut = Application.Instance.CommonModifier | Keys.F, Image = ControlGenerator.GetIcon("Search", _log) };
            searchProject.Executed += Search_Executed;

            // Build
            Command buildIterativeProject = new() { MenuText = "Build", ToolBarText = "Build", Image = ControlGenerator.GetIcon("Build", _log) };
            buildIterativeProject.Executed += BuildIterativeProject_Executed;

            Command buildBaseProject = new() { MenuText = "Build from Scratch", ToolBarText = "Build from Scratch", Image = ControlGenerator.GetIcon("Build_Scratch", _log) };
            buildBaseProject.Executed += BuildBaseProject_Executed;
            
            Command buildAndRunProject = new() { MenuText = "Build and Run", ToolBarText = "Run", Image = ControlGenerator.GetIcon("Build_Run", _log) };
            buildAndRunProject.Executed += BuildAndRunProject_Executed;

            // Application Items
            Command preferencesCommand = new();
            preferencesCommand.Executed += PreferencesCommand_Executed;

            // About
            Command aboutCommand = new() { MenuText = "About..." };
            AboutDialog aboutDialog = new() { ProgramName = "Serial Loops", Developers = new[] { "Jonko", "William" }, Copyright = "© Haroohie Translation Club, 2023", Website = new Uri("https://haroohie.club")  };
            aboutCommand.Executed += (sender, e) => aboutDialog.ShowDialog(this);

            // create menu
            Menu = new MenuBar
            {
                Items =
                {
                    // File submenu
                    new SubMenuItem { Text = "&File", Items = { newProject, openProject, _recentProjects, saveProject } },
                    new SubMenuItem { Text = "&Tools", Items = { searchProject } },
                    // new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
                    // new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
                    new SubMenuItem { Text = "&Build", Items = { buildIterativeProject, buildBaseProject, buildAndRunProject } },
                },
                ApplicationItems =
                {
                    // application (OS X) or file menu (others)
                    new ButtonMenuItem { Text = "&Preferences...", Command = preferencesCommand },
                },
                AboutItem = aboutCommand
            };

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
        }

        private void OpenProjectView(Project project, IProgressTracker tracker)
        {
            EditorTabs = new(project);
            ItemExplorer = new(project, EditorTabs, _log);
            Title = $"{BASE_TITLE} - {project.Name}";
            Content = new TableLayout(new TableRow(ItemExplorer, EditorTabs));
            LoadCachedData(project, tracker);    
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
            }
        }

        private void UpdateRecentProjects()
        {
            _recentProjects.Items.Clear();
            foreach (string project in ProjectsCache.RecentProjects)
            {
                Command recentProject = new() { MenuText = Path.GetFileNameWithoutExtension(project), ToolTip = project };
                recentProject.Executed += OpenRecentProject_Executed;
                if (!File.Exists(project))
                {
                    recentProject.Enabled = false;
                    recentProject.MenuText += " (Missing)";
                    recentProject.Image = ControlGenerator.GetIcon("Warning", _log);
                }
                _recentProjects.Items.Add(recentProject);
            }
            _recentProjects.Enabled = _recentProjects.Items.Count > 0;
        }

        private void NewProjectCommand_Executed(object sender, EventArgs e)
        {
            ProjectCreationDialog projectCreationDialog = new() { Config = CurrentConfig, Log = _log };
            projectCreationDialog.ShowModal(this);
            if (projectCreationDialog.NewProject is not null)
            {
                OpenProject = projectCreationDialog.NewProject;
                OpenProjectView(OpenProject, new LoopyProgressTracker());
            }
        }

        private void OpenProject_Executed(object sender, EventArgs e)
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

        private void OpenProjectFromPath(string path)
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
            foreach (ItemDescription item in unsavedItems)
            {
                switch (item.Type)
                {
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
                        foreach (Editor editor in EditorTabs.Tabs.Pages.Cast<Editor>())
                        {
                            editor.UpdateTabTitle(true);
                        }
                        break;
                    case ItemDescription.ItemType.Script:
                        EventFile evt = ((ScriptItem)item).Event;
                        evt.CollectGarbage();
                        IO.WriteStringFile(Path.Combine("assets", "events", $"{evt.Index:X3}.s"), evt.GetSource(new()), OpenProject, _log);
                        foreach (Editor editor in EditorTabs.Tabs.Pages.Cast<Editor>())
                        {
                            editor.UpdateTabTitle(true);
                        }
                        break;

                    default:
                        _log.LogWarning($"Saving for {item.Type}s not yet implemented.");
                        break;
                }
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

        private void BuildIterativeProject_Executed(object sender, EventArgs e)
        {
            if (OpenProject is not null)
            {
                bool buildSucceeded = true; // imo it's better to have a false negative than a false positive here
                LoopyProgressTracker tracker = new("Building:");
                ProgressDialog loadingDialog = new(async () => buildSucceeded = await Build.BuildIterative(OpenProject, CurrentConfig, _log, tracker), () =>
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
                ProgressDialog loadingDialog = new(async () => buildSucceeded = await Build.BuildBase(OpenProject, CurrentConfig, _log, tracker), () =>
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
                ProgressDialog loadingDialog = new(async () => buildSucceeded = await Build.BuildIterative(OpenProject, CurrentConfig, _log, tracker), () =>
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

        private void PreferencesCommand_Executed(object sender, EventArgs e)
        {
            PreferencesDialog preferencesDialog = new(CurrentConfig, _log);
            preferencesDialog.ShowModal(this);
            CurrentConfig = preferencesDialog.Configuration;
        }

        public void MainForm_Closed(object sender, EventArgs e)
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
                ProjectsCache.Save(_log);
            }
        }
    }
}
