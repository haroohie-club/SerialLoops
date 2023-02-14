using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Controls;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SerialLoops
{
    public partial class MainForm : Form
    {
        private const string BASE_TITLE = "Serial Loops";

        private LoopyLogger _log;
        public RecentProjects RecentProjects { get; set; }
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
                Items =
                {
                    buildIterativeProject,
                    buildAndRunProject,
                    searchProject
                }
            };
        }

        private void OpenProjectView(Project project)
        {
            EditorTabs = new(project);
            ItemExplorer = new(project, EditorTabs, _log);
            Title = $"{BASE_TITLE} - {project.Name}";
            Content = new TableLayout(new TableRow(ItemExplorer, EditorTabs));
            try
            {
                RecentProjects.AddProject(Path.Combine(project.MainDirectory, $"{project.Name}.{Project.PROJECT_FORMAT}"));
                RecentProjects.Save(_log);
                UpdateRecentProjects();
            }
            catch (Exception e)
            {
                _log.LogError($"Failed to add project to recent projects list: {e.Message}");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _log = new();
            CurrentConfig = Config.LoadConfig(_log);
            RecentProjects = RecentProjects.LoadRecentProjects(_log);
            UpdateRecentProjects();
        }

        private void UpdateRecentProjects()
        {
            _recentProjects.Items.Clear();
            foreach (string project in RecentProjects.Projects)
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
                OpenProjectView(OpenProject);
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
            LoopyProgressTracker tracker = new ();
            new ProgressDialog(() => OpenProject = Project.OpenProject(path, CurrentConfig, _log, tracker), () => OpenProjectView(OpenProject), tracker, "Loading Project");
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
                    case ItemDescription.ItemType.Script:
                        EventFile evt = ((ScriptItem)item).Event;
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
                bool buildSucceeded = false;
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
                bool buildSucceeded = false;
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

                bool buildSucceeded = false;
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
    }
}
