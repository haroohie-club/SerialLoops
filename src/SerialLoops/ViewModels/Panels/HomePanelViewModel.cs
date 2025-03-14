using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Emik;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Utility;

namespace SerialLoops.ViewModels.Panels;

public class HomePanelViewModel : ViewModelBase
{
    public MainWindowViewModel MainWindow { get; set; }
    public ILogger Log => MainWindow.Log;

    public ObservableCollection<RecentProjectViewModel> RecentProjects { get; private set; }


    public HomePanelViewModel(MainWindowViewModel mainWindow)
    {
        MainWindow = mainWindow;
        RecentProjects = new(MainWindow.ProjectsCache.RecentProjects.Where(p => !MainWindow.CurrentConfig.RemoveMissingProjects || File.Exists(p))
            .Select(p => new RecentProjectViewModel(p, this)));
    }
}

public class RecentProjectViewModel : ReactiveObject
{
    [Reactive]
    public string Text { get; set; }
    public ICommand Command { get; }
    public bool IsMissing { get; set; }
    public string IconPath { get; set; }
    public string LinkColorKey { get; set; }

    public ICommand RenameCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand DeleteCommand { get; }

    public RecentProjectViewModel(string path, HomePanelViewModel parent)
    {
        Text = Path.GetFileName(path);
        Command = ReactiveCommand.Create(() => parent.MainWindow.OpenRecentProjectCommand.Execute(path));
        IsMissing = !File.Exists(path);
        IconPath = IsMissing ? "avares://SerialLoops/Assets/Icons/Warning.svg" : "avares://SerialLoops/Assets/Icons/AppIconSimple.svg";
        LinkColorKey = IsMissing ? "DisabledLinkColor" : "LinkColor";

        RenameCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            
        });

        DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (await parent.MainWindow.Window.ShowMessageBoxAsync(Strings.ProjectDeleteConfirmTitle,
                    Strings.ProjectDeleteConfirmText,
                    ButtonEnum.YesNoCancel, Icon.Warning, parent.MainWindow.Log) == ButtonResult.Yes)
            {
                if (!await Rubbish.MoveAsync(Path.GetDirectoryName(path)))
                {
                    if (await parent.MainWindow.Window.ShowMessageBoxAsync(Strings.ProjectDeleteFailedTitle,
                            Strings.ProjectDeleteFailedText,ButtonEnum.YesNoCancel, Icon.Error, parent.MainWindow.Log) == ButtonResult.Yes)
                    {
                        Directory.Delete(Path.GetDirectoryName(path)!, true);
                    }
                    else
                    {
                        return; // don't remove this project if we didn't delete it
                    }
                }
                parent.RecentProjects.Remove(this);
                parent.MainWindow.ProjectsCache.RemoveProject(path);
                parent.MainWindow.ProjectsCache.Save(parent.MainWindow.Log);
            }
        });
    }
}
