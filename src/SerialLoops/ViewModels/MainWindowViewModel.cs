using Avalonia;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SerialLoops.Views;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SerialLoops.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Title { get; set; } = "Serial Loops";
        public Size MinSize => new(769, 420);
        public Size ClientSize { get; set; } = new(1200, 800);

        public MainWindow Window { get; set; }
        public ProjectsCache ProjectsCache { get; set; }
        public Config CurrentConfig { get; set; }
        public Project OpenProject { get; set; }

        public NativeMenuItem RecentProjectsMenu { get; set; } = new(Strings.Recent_Projects);
        public LoopyLogger Log { get; set; }

        public void Initialize(MainWindow window)
        {
            Window = window;
            Log = new();
            CurrentConfig = Config.LoadConfig(Log);
            Strings.Culture = new(CurrentConfig.CurrentCultureName);
            Log.Initialize(CurrentConfig);

            ProjectsCache = ProjectsCache.LoadCache(CurrentConfig, Log);
            UpdateRecentProjects();

            if (CurrentConfig.CheckForUpdates)
            {
                new UpdateChecker(this).Check();
            }


        }

        public async Task CloseProject_Executed(WindowClosingEventArgs e)
        {
            if (OpenProject is not null)
            {
                // Warn against unsaved items
                IEnumerable<ItemDescription> unsavedItems = OpenProject.Items.Where(i => i.UnsavedChanges);
                if (unsavedItems.Any())
                {
                    ButtonResult result;
                    bool skipBuild = false;
                    if (e.CloseReason == WindowCloseReason.OSShutdown) // if the OS is shutting down, we're going to expedite things
                    {
                        result = ButtonResult.Yes;
                        skipBuild = true;
                    }
                    else
                    {
                        // message box with yes no cancel buttons
                        result = await MessageBoxManager.GetMessageBoxStandard(Strings.Confirm,
                            string.Format(Strings.You_have_unsaved_changes_in__0__item_s___Would_you_like_to_save_before_closing_the_project_, unsavedItems.Count()),
                            ButtonEnum.YesNoCancel, Icon.Warning, WindowStartupLocation.CenterScreen).ShowAsPopupAsync(Window);
                    }
                    switch (result)
                    {
                        case ButtonResult.Yes:
                            //SaveProject_Executed(sender, e);
                            if (!skipBuild)
                            {
                                //BuildIterativeProject_Executed(sender, e); // make sure we lock in the changes
                            }
                            break;
                        default:
                            e.Cancel = true;
                            break;
                    }
                }

                // Record open items
                //List<string> openItems = EditorTabs.Tabs.Pages.Cast<Editor>()
                //    .Select(e => e.Description)
                //    .Select(i => i.Name)
                //    .ToList();
                //ProjectsCache.CacheRecentProject(OpenProject.ProjectFile, openItems);
                //ProjectsCache.HadProjectOpenOnLastClose = true;
                //ProjectsCache.Save(Log);
            }
        }
        private void UpdateRecentProjects()
        {
            RecentProjectsMenu.Menu = [];

            List<string> projectsToRemove = [];
            foreach (string project in ProjectsCache.RecentProjects)
            {
                NativeMenuItem recentProject = new() { Header = Path.GetFileNameWithoutExtension(project), ToolTip = project };
                //recentProject.Command += OpenRecentProject_Executed;
                if (!File.Exists(project))
                {
                    if (CurrentConfig.RemoveMissingProjects)
                    {
                        projectsToRemove.Add(project);
                        continue;
                    }

                    recentProject.IsEnabled = false;
                    recentProject.Header += Strings.Missing_;
                    recentProject.Icon = ControlGenerator.GetIcon("Warning", Log);
                }

                RecentProjectsMenu.Menu.Items.Add(recentProject);
            }

            RecentProjectsMenu.IsEnabled = RecentProjectsMenu.Menu.Items.Count > 0;

            projectsToRemove.ForEach(project =>
            {
                ProjectsCache.RecentProjects.Remove(project);
                ProjectsCache.RecentWorkspaces.Remove(project);
            });
            ProjectsCache.Save(Log);
        }
    }
}
