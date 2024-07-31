using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs
{
    public partial class ProjectCreationDialogViewModel : ViewModelBase
    {
        private ILogger _log;
        private MainWindowViewModel _mainWindow;
        private Config _config;

        private string _romPath = Strings.None_Selected;

        public string ProjectName { get; set; }
        public ComboBoxItem LanguageTemplateItem { get; set; }
        public string LanguageTemplateCode => (string)LanguageTemplateItem?.Tag ?? "";
        public string RomPath
        {
            get => _romPath;
            set => SetProperty(ref _romPath, value);
        }

        public ICommand PickRomCommand { get; set; }
        public ICommand CreateCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public ProjectCreationDialogViewModel(Config config, MainWindowViewModel mainWindow, ILogger log)
        {
            _log = log;
            _mainWindow = mainWindow;
            _config = config;
            PickRomCommand = ReactiveCommand.CreateFromTask(PickRom);
            CreateCommand = ReactiveCommand.CreateFromTask<ProjectCreationDialog>(CreateProject);
            CancelCommand = ReactiveCommand.Create<ProjectCreationDialog>((dialog) => dialog.Close());
        }

        private async Task PickRom()
        {
            FilePickerOpenOptions options = new()
            {
                Title = Strings.Open_ROM,
                FileTypeFilter = [new FilePickerFileType(Strings.Chokuretsu_ROM) { Patterns = [ "*.nds" ] }]
            };
            IStorageFile rom = (await _mainWindow.Window.StorageProvider.OpenFilePickerAsync(options)).FirstOrDefault();
            if (rom is not null)
            {
                RomPath = rom.Path.AbsolutePath;
            }
        }

        private async Task CreateProject(ProjectCreationDialog dialog)
        {
            if (string.IsNullOrEmpty(RomPath) || RomPath.Equals(Strings.None_Selected))
            {
                await MessageBoxManager.GetMessageBoxStandard(Strings.Project_Creation_Warning, Strings.Please_select_a_ROM_before_creating_the_project_,
                    ButtonEnum.Ok, Icon.Warning).ShowWindowDialogAsync(_mainWindow.Window);
            }
            else if (string.IsNullOrWhiteSpace(ProjectName))
            {
                await MessageBoxManager.GetMessageBoxStandard(Strings.Project_Creation_Warning, Strings.Please_choose_a_project_name_before_creating_the_project_,
                    ButtonEnum.Ok, Icon.Warning).ShowWindowDialogAsync(_mainWindow.Window);
            }
            else
            {
                Project newProject = new(ProjectName, LanguageTemplateCode, _config, (s) => s, _log);
                LoopyProgressTracker tracker = new();
                await new ProgressDialog(() =>
                {
                    ((IProgressTracker)tracker).Focus(Strings.Creating_Project, 1);
                    Dispatcher.UIThread.Post(() =>
                    {
                        Lib.IO.OpenRom(newProject, RomPath, _log, tracker);
                    });
                    tracker.Finished++;
                    newProject.Load(_config, _log, tracker);
                }, () => dialog.Close(newProject), tracker, Strings.Creating_Project).ShowDialog(_mainWindow.Window);
            }
        }
    }
}
