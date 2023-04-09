using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Utility;
using System.IO;

namespace SerialLoops.Controls
{
    public class HomePanel : Panel
    {

        private readonly MainForm _mainForm;
        private readonly ILogger _log;

        public HomePanel(MainForm mainForm, ILogger log) {
            _mainForm = mainForm;
            _log = log;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            StackLayout appTitleLayout = new()
            {
                Padding = 15,
                Spacing = 5,
                Orientation = Orientation.Vertical,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    new ImageView { Image = ControlGenerator.GetIcon("AppIcon", _log, 64) },
                    ControlGenerator.GetTextHeader("Serial Loops", 18)
                }
            };

            LinkButton newProjectLink = new() { Text = "New Project" };
            newProjectLink.Click += _mainForm.NewProjectCommand_Executed;

            LinkButton openProjectLink = new() { Text = "Open Project" };
            openProjectLink.Click += _mainForm.OpenProject_Executed;

            LinkButton preferencesLink = new() { Text = "Preferences" };
            preferencesLink.Click += _mainForm.PreferencesCommand_Executed;

            StackLayout startPanel = new()
            {
                Padding = 15,
                Spacing = 10,
                MinimumSize = new(150, 400),
                Items =
                {
                    ControlGenerator.GetTextHeader("Start"),
                    ControlGenerator.GetControlWithIcon(newProjectLink, "New", _log),
                    ControlGenerator.GetControlWithIcon(openProjectLink, "Open", _log),
                    ControlGenerator.GetControlWithIcon(preferencesLink, "Options", _log)
                }
            };

            Content = new StackLayout
            {
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    appTitleLayout,
                    new TableLayout(new TableRow(startPanel, GetRecentProjectsPanel())),
                }
            };
        }

        private StackLayout GetRecentProjectsPanel()
        {
            StackLayout recentProjects = new()
            {
                Padding = 15,
                Spacing = 10,
                MinimumSize = new(350, 400),
                Items = { ControlGenerator.GetTextHeader("Recents") }
            };

            StackLayout projectList = new() { Spacing = 5 };
            foreach (string project in _mainForm.ProjectsCache.RecentProjects)
            {
                bool missing = !File.Exists(project);
                LinkButton button = new() { Text = Path.GetFileName(project) };
                button.Click += (sender, args) => _mainForm.OpenProjectFromPath(project);
                if (missing && _mainForm.CurrentConfig.RemoveMissingProjects) { continue; }

                button.Enabled = !missing;
                projectList.Items.Add(ControlGenerator.GetControlWithIcon(new StackLayout()
                {
                    Spacing = 10,
                    Padding = 5,
                    Orientation = Orientation.Horizontal,
                    Items =
                    {
                        button,
                        new Label() { Text = (missing ? "Missing: " : "") + project, TextColor = Color.FromGrayscale(0.5f) }
                    }
                }, !missing ? "AppIcon" : "Warning", _log));
            }

            if (projectList.Items.Count is 0)
            {
                recentProjects.Items.Add("No recent projects. Create one and it will appear here.");
            } 
            else
            {
                recentProjects.Items.Add(projectList);
            }

            return recentProjects;
        }

    }
}
