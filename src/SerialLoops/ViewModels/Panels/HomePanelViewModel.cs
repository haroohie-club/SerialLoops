using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using DynamicData;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Panels;

public class HomePanelViewModel : ViewModelBase
{
    public MainWindowViewModel MainWindow { get; set; }
    public ILogger Log => MainWindow.Log;

    public ObservableCollection<RecentProjectViewModel> RecentProjects { get; }

    [Reactive]
    public bool DisplayFirstTimeFlatpakMessage { get; set; }

    public ICommand ImportProjectsCommand { get; }


    public HomePanelViewModel(MainWindowViewModel mainWindow)
    {
        MainWindow = mainWindow;
        RecentProjects = new(MainWindow.ProjectsCache.RecentProjects.Where(p => !MainWindow.CurrentConfig.RemoveMissingProjects || File.Exists(p))
            .Select(p => new RecentProjectViewModel(p, this)));
        DisplayFirstTimeFlatpakMessage = MainWindow.CurrentConfig.FirstTimeFlatpak && !RecentProjects.Any();
        MainWindow.CurrentConfig.FirstTimeFlatpak = false;
        ImportProjectsCommand = ReactiveCommand.CreateFromTask(ImportProjectsFolderToSandbox);
    }

    private async Task ImportProjectsFolderToSandbox()
    {
        string oldProjectsFolder = (await MainWindow.Window.ShowOpenFolderPickerAsync(Strings.FirstTimeFlatpakButton))
            .TryGetLocalPath();
        if (!string.IsNullOrEmpty(oldProjectsFolder))
        {
            Lib.IO.CopyDirectoryRecursively(oldProjectsFolder, MainWindow.CurrentConfig.ProjectsDirectory);
        }
        RecentProjects.Clear();
        RecentProjects.AddRange(Directory.GetFiles(MainWindow.CurrentConfig.ProjectsDirectory, "*.slproj", SearchOption.AllDirectories)
            .Select(p => new RecentProjectViewModel(p, this)));
        DisplayFirstTimeFlatpakMessage = false;
    }
}

public class RecentProjectViewModel : ReactiveObject
{
    [Reactive]
    public string Text { get; set; }
    public bool IsMissing { get; set; }
    public string IconPath { get; set; }
    public string LinkColorKey { get; set; }

    [Reactive]
    public ICommand OpenCommand { get; private set; }
    [Reactive]
    public ICommand RenameCommand { get; private set; }
    [Reactive]
    public ICommand DuplicateCommand { get; private set; }
    [Reactive]
    public ICommand DeleteCommand { get; private set; }

    public RecentProjectViewModel(string path, HomePanelViewModel parent)
    {
        Text = Path.GetFileName(path);
        OpenCommand = ReactiveCommand.Create(() => parent.MainWindow.OpenRecentProjectCommand.Execute(path));
        IsMissing = !File.Exists(path);
        IconPath = IsMissing ? "avares://SerialLoops/Assets/Icons/Warning.svg" : "avares://SerialLoops/Assets/Icons/AppIconSimple.svg";
        LinkColorKey = IsMissing ? "DisabledLinkColor" : "LinkColor";

        RenameCommand = ReactiveCommand.CreateFromTask(async () => await RenameTask(path, parent));

        DuplicateCommand = ReactiveCommand.CreateFromTask(async () => await Duplicate(path, parent));

        DeleteCommand = ReactiveCommand.CreateFromTask(async () => await Delete(path, parent));
    }

    public void Rename(string newProj, HomePanelViewModel parent)
    {
        Text = Path.GetFileName(newProj);
        OpenCommand = ReactiveCommand.Create(() => parent.MainWindow.OpenRecentProjectCommand.Execute(newProj));
        RenameCommand = ReactiveCommand.CreateFromTask(async () => await RenameTask(newProj, parent));
        DuplicateCommand = ReactiveCommand.CreateFromTask(async () => await Duplicate(newProj, parent));
        DeleteCommand = ReactiveCommand.CreateFromTask(async () => await Delete(newProj, parent));
    }

    private async Task RenameTask(string path, HomePanelViewModel parent)
    {
        ProjectRenameDuplicateDialogViewModel renameDialogViewModel =
            new(true, path, parent.MainWindow.CurrentConfig, parent.MainWindow.Log);
        string newProj = await new ProjectRenameDuplicateDialog { DataContext = renameDialogViewModel }.ShowDialog<string>(parent.MainWindow.Window);
        if (string.IsNullOrEmpty(newProj))
        {
            return;
        }
        Rename(newProj, parent);
        parent.MainWindow.ProjectsCache.RenameProject(path, newProj);
        parent.MainWindow.ProjectsCache.Save(parent.MainWindow.Log);
    }

    private async Task Duplicate(string path, HomePanelViewModel parent)
    {
        ProjectRenameDuplicateDialogViewModel renameDialogViewModel =
            new(false, path, parent.MainWindow.CurrentConfig, parent.MainWindow.Log);
        string newProj = await new ProjectRenameDuplicateDialog { DataContext = renameDialogViewModel }.ShowDialog<string>(parent.MainWindow.Window);
        if (string.IsNullOrEmpty(newProj))
        {
            return;
        }

        if (await parent.MainWindow.Window.ShowMessageBoxAsync(Strings.ProjectDuplicatedSuccessTitle,
                Strings.ProjectDuplicatedSuccessText, ButtonEnum.YesNo, Icon.Success, parent.MainWindow.Log) == ButtonResult.Yes)
        {
            parent.MainWindow.OpenRecentProjectCommand.Execute(newProj);
        }
    }

    private async Task Delete(string path, HomePanelViewModel parent)
    {
        if (await Shared.DeleteProjectAsync(path, parent.MainWindow))
        {
            parent.RecentProjects.Remove(this);
        }
    }
}
