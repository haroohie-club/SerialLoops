using Eto.Forms;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Controls;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SerialLoops
{
    public partial class MainForm : Form
    {
        private const string BASE_TITLE = "Serial Loops";

        private LoopyLogger _log;
        public Config CurrentConfig { get; set; }
        public Project OpenProject { get; set; }
        public EditorTabsPanel EditorTabs { get; set; }
        public ItemExplorerPanel ItemExplorer { get; set; }

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
            Command searchProject = new() { MenuText = "Search", ToolBarText = "Search", Shortcut = Application.Instance.CommonModifier | Keys.F };
            searchProject.Executed += Search_Executed;

            // Build
            Command buildIterativeProject = new() { MenuText = "Build", ToolBarText = "Build" };
            buildIterativeProject.Executed += BuildIterativeProject_Executed;

            Command buildBaseProject = new() { MenuText = "Build from Scratch", ToolBarText = "Build from Scratch" };
            buildBaseProject.Executed += BuildBaseProject_Executed;

            // Application Items
            Command preferencesCommand = new();
            preferencesCommand.Executed += PreferencesCommand_Executed;

            // About
            Command aboutCommand = new() { MenuText = "About..." };
            AboutDialog aboutDialog = new() { ProgramName = "Serial Loops", Developers = new string[] { "Jonko", "William" }, Copyright = "© Haroohie Translation Club, 2023", Website = new Uri("https://haroohie.club")  };
            aboutCommand.Executed += (sender, e) => aboutDialog.ShowDialog(this);

            // create menu
            Menu = new MenuBar
            {
                Items =
                {
                    // File submenu
                    new SubMenuItem { Text = "&File", Items = { newProject, openProject, saveProject } },
                    new SubMenuItem { Text = "&Tools", Items = { searchProject } },
                    // new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
                    // new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
                    new SubMenuItem { Text = "&Build", Items = { buildIterativeProject, buildBaseProject } },
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
                    buildIterativeProject
                }
            };
        }

        private void OpenProjectView(Project project)
        {
            EditorTabs = new(project);
            ItemExplorer = new(project, EditorTabs, _log);
            Title = $"{BASE_TITLE} - {project.Name}";
            Content = new TableLayout(new TableRow(ItemExplorer, EditorTabs));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _log = new();
            CurrentConfig = Config.LoadConfig(_log);
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
                OpenProject = Project.OpenProject(openFileDialog.FileName, CurrentConfig, _log);
                OpenProjectView(OpenProject);
            }
        }

        private void SaveProject_Executed(object sender, EventArgs e)
        {
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

        private async void BuildIterativeProject_Executed(object sender, EventArgs e)
        {
            if (await Build.BuildIterative(OpenProject, CurrentConfig, _log))
            {
                MessageBox.Show("Build succeeded!");
            }
            else
            {
                MessageBox.Show("Build failed!");
            }
        }

        private async void BuildBaseProject_Executed(object sender, EventArgs e)
        {
            if (await Build.BuildBase(OpenProject, CurrentConfig, _log))
            {
                MessageBox.Show("Build succeeded!");
            }
            else
            {
                MessageBox.Show("Build failed!");
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
