using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public partial class ProjectCreationDialogViewModel : ViewModelBase
{
    private ILogger _log;
    private MainWindowViewModel _mainWindow;
    private ConfigUser _configUser;

    public string ProjectName { get; set; }
    public ComboBoxItem LanguageTemplateItem { get; set; }
    public string LanguageTemplateCode => (string)LanguageTemplateItem?.Tag ?? "";
    [Reactive]
    public string RomPath { get; set; } = Strings.ProjectCreationNoRomSelected;

    public bool Migrate { get; set; }

    public string Title => Migrate ? Strings.MenuMigrateProject : Strings.ProjectCreationCreateNewProjectTitle;
    public string ButtonText => Migrate ? Strings.ProjectMigrateMigrateButton : Strings.ProjectCreateButtonLabel;

    public ICommand PickRomCommand { get; set; }
    public ICommand CreateCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    public ProjectCreationDialogViewModel(ConfigUser configUser, MainWindowViewModel mainWindow, ILogger log, bool migrate = false)
    {
        _log = log;
        _mainWindow = mainWindow;
        _configUser = configUser;
        Migrate = migrate;
        PickRomCommand = ReactiveCommand.CreateFromTask(PickRom);
        CreateCommand = Migrate ? ReactiveCommand.Create<ProjectCreationDialog>(dialog => dialog.Close((RomPath, LanguageTemplateCode))) : ReactiveCommand.CreateFromTask<ProjectCreationDialog>(CreateProject);
        CancelCommand = ReactiveCommand.Create<ProjectCreationDialog>((dialog) => dialog.Close());
    }

    private async Task PickRom()
    {
        IStorageFile rom = await _mainWindow.Window.ShowOpenFilePickerAsync(Strings.RomOpenFileDialogTitle, [new(Strings.ProjectCreationChokuretsuROMLabel) { Patterns = ["*.nds"] }]);
        if (rom is not null)
        {
            RomPath = rom.Path.LocalPath;
        }
    }

    private async Task CreateProject(ProjectCreationDialog dialog)
    {
        if (string.IsNullOrEmpty(RomPath) || RomPath.Equals(Strings.ProjectCreationNoRomSelected))
        {
            await _mainWindow.Window.ShowMessageBoxAsync(Strings.ProjectCreationWarning, Strings.ProjectCreationNoROMSelectedMessage, ButtonEnum.Ok, Icon.Warning, _log);
        }
        else if (string.IsNullOrWhiteSpace(ProjectName))
        {
            await _mainWindow.Window.ShowMessageBoxAsync(Strings.ProjectCreationWarning, Strings.ProjectCreationNoNameMessage, ButtonEnum.Ok, Icon.Warning, _log);
        }
        else
        {
            Project newProject = new(ProjectName, LanguageTemplateCode, _configUser, Strings.ResourceManager.GetString, _log);
            ProgressDialogViewModel tracker = new(Strings.ProjectCreationProgressMessage);
            tracker.InitializeTasks(() =>
            {
                ((IProgressTracker)tracker).Focus(Strings.ProjectCreationProgressMessage, 1);
                Lib.IO.OpenRom(newProject, RomPath, _log, tracker);
                tracker.Finished++;
                newProject.Load(_configUser, _log, tracker);
                newProject.SetBaseRomHash(RomPath);
                newProject.Save(_log);
            }, () => dialog.Close(newProject));
            await new ProgressDialog { DataContext = tracker }.ShowDialog(_mainWindow.Window);
        }
    }
}
