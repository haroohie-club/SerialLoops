using Eto.Forms;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Utility;
using System;

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

            // Tools
            Command searchProject = new() { MenuText = "Search", ToolBarText = "Search", Shortcut = Application.Instance.CommonModifier | Keys.F };
            searchProject.Executed += Search_Executed;

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
                    new SubMenuItem { Text = "&Tools", Items = { searchProject } },
                    // new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
                    // new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
                    //new SubMenuItem { Text = "&Build", Items = { } },
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
    }
}
