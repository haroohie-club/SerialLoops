using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;

namespace SerialLoops.ViewModels.Panels;

public partial class HomePanelViewModel : ViewModelBase
{
    public MainWindowViewModel MainWindow { get; set; }
    public ILogger Log => MainWindow.Log;

    public ObservableCollection<RecentProjectViewModel> RecentProjects { get; private set; }


    public HomePanelViewModel(MainWindowViewModel mainWindow)
    {
        MainWindow = mainWindow;
        RecentProjects = new(MainWindow.ProjectsCache.RecentProjects.Where(p => !MainWindow.CurrentConfig.RemoveMissingProjects || File.Exists(p))
            .Select(p => new RecentProjectViewModel(p, mainWindow)));
    }
}

public class RecentProjectViewModel
{
    public string Text { get; set; }
    public ICommand Command { get; }
    public bool IsMissing { get; set; }
    public string IconPath { get; set; }
    public string LinkColorKey { get; set; }

    public RecentProjectViewModel(string path, MainWindowViewModel mainWindow)
    {
        Text = Path.GetFileName(path);
        Command = ReactiveCommand.Create(() => mainWindow.OpenRecentProjectCommand.Execute(path));
        IsMissing = !File.Exists(path);
        IconPath = IsMissing ? "avares://SerialLoops/Assets/Icons/Warning.svg" : "avares://SerialLoops/Assets/Icons/AppIconSimple.svg";
        LinkColorKey = IsMissing ? "DisabledLinkColor" : "LinkColor";
    }
}
