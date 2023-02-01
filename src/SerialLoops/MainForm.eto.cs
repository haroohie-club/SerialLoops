using Eto.Forms;
using SerialLoops.Lib;
using System;

namespace SerialLoops
{
    public partial class MainForm : Form
    {
        private const string BASE_TITLE = "Serial Loops";

        private LoopyLogger _log;
        public Config CurrentConfig { get; set; }
        public Project OpenProject { get; set; }

        void InitializeComponent()
        {
            Title = BASE_TITLE;
            ClientSize = new(769, 420);
            MinimumSize = new(769, 420);
            Padding = 10;


            // Commands
            // File
            Command newProject = new() { MenuText = "New Project", ToolBarText = "New Project" };
            newProject.Executed += NewProjectCommand_Executed;

            Command openProject = new() { MenuText = "Open Project", ToolBarText = "Open Project" };
            openProject.Executed += OpenProject_Executed;

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
					new SubMenuItem { Text = "&File", Items = { newProject, openProject } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
                    new SubMenuItem { Text = "&Build", Items = { } },
				},
                ApplicationItems =
                {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
                },
                AboutItem = aboutCommand
            };
        }

        private void OpenProjectView(Project project)
        {
            EditorTabsPanel tabs = new(project);
            ItemExplorerPanel items = new(project, tabs, _log);
            Title = $"{BASE_TITLE} - {project.Name}";
            Content = new StackLayout
            {
                Items =
                {
                    new Splitter
                    {
                        Orientation = Orientation.Horizontal,
                        Panel1 = items,
                        Panel2 = tabs
                    }
                }
            };
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
            if (openFileDialog.ShowDialog(this) == DialogResult.Ok)
            {
                OpenProject = Project.OpenProject(openFileDialog.FileName, CurrentConfig, _log);
                OpenProjectView(OpenProject);
            }
        }
    }
}
