using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Immutable;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Utility;
using SerialLoops.Views.Panels;

namespace SerialLoops.ViewModels.Panels
{
    public partial class HomePanelViewModel : ViewModelBase
    {
        public MainWindowViewModel MainWindow { get; set; }
        public ILogger Log => MainWindow.Log;
        private HomePanel _homePanel;

        public void Initialize(MainWindowViewModel mainWindow, HomePanel homePanel)
        {
            MainWindow = mainWindow;
            _homePanel = homePanel;
            foreach (string project in MainWindow.ProjectsCache.RecentProjects)
            {
                bool missing = !File.Exists(project);
                if (missing && MainWindow.CurrentConfig.RemoveMissingProjects)
                {
                    continue;
                }
                LinkButton linkButton = new()
                {
                    Text = Path.GetFileName(project),
                    OnClick = (sender, args) =>
                    {
                        MainWindow.OpenRecentProjectCommand.Execute(project);
                    },
                    IsEnabled = !missing,
                };

                StackPanel panel = new()
                {
                    Spacing = 10,
                    Orientation = Orientation.Horizontal,
                };
                panel.Children.Add(linkButton);
                if (missing)
                {
                    MainWindow.Window.TryFindResource("DisabledLinkColor", MainWindow.Window.ActualThemeVariant, out object? brush);
                    panel.Children.Add(new TextBlock { Text = Strings.Missing_ + $" {project}", Foreground = (ImmutableSolidColorBrush)brush });
                }
                _homePanel.RecentsPanel.Children.Add(ControlGenerator.GetControlWithIcon(panel, !missing ? "AppIconSimple" : "Warning", Log));
            }

            if (_homePanel.RecentsPanel.Children.Count == 0)
            {
                _homePanel.RecentsPanel.Children.Add(new TextBlock() { Text = Strings.No_recent_projects__Create_one_and_it_will_appear_here_ });
            }
        }
    }
}
