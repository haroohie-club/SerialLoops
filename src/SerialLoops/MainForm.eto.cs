using Eto.Drawing;
using Eto.Forms;
using System;
using System.IO;

namespace SerialLoops
{
    public partial class MainForm : Form
    {
        private const string BASE_TITLE = "Serial Loops";

        public Config CurrentConfig { get; set; }
        public Project OpenProject { get; set; }

        void InitializeComponent()
        {
            Title = BASE_TITLE;
            ClientSize = new(769, 420);
            MinimumSize = new(769, 420);
            Padding = 10;

            Content = new StackLayout
            {
                Items =
                {
                    "Hello World!",
					// add more controls here
				}
            };

            // Commands
            Command newProject = new() { MenuText = "New Project", ToolBarText = "New Project" };
            newProject.Executed += NewProjectCommand_Executed;

            Command openProject = new() { MenuText = "Open Project", ToolBarText = "Open Project" };
            openProject.Executed += OpenProject_Executed;

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
				},
                ApplicationItems =
                {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
                },
                AboutItem = aboutCommand
            };
        }

        private void RenameWindow(string subtitle)
        {
            Title = $"{BASE_TITLE} - {subtitle}";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CurrentConfig = Config.LoadConfig();
        }

        private void NewProjectCommand_Executed(object sender, EventArgs e)
        {
            ProjectCreationDialog projectCreationDialog = new() { Config = CurrentConfig };
            projectCreationDialog.ShowModal(this);
            if (projectCreationDialog.NewProject is not null)
            {
                OpenProject = projectCreationDialog.NewProject;
                RenameWindow(OpenProject.Name);
            }
        }

        private void OpenProject_Executed(object sender, EventArgs e)
        {
            SelectFolderDialog selectFolderDialog = new() { Directory = CurrentConfig.ProjectsDirectory };
            if (selectFolderDialog.ShowDialog(this) == DialogResult.Ok)
            {
                OpenProject = Project.OpenProject(Path.GetFileNameWithoutExtension(selectFolderDialog.Directory), CurrentConfig);
                RenameWindow(OpenProject.Name);
            }
        }
    }
}
