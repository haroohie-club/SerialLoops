using System.Diagnostics;
using System.Drawing;
using System.Text.Json.Nodes;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs
{
    public class UpdateAvailableDialogViewModel : ViewModelBase
    {
        public Size MinSize => new(600, 375);
        public Size ClientSize { get; set; } = new(1000, 650);
        public Size GridSize => new(ClientSize.Width - 40, ClientSize.Height - 40);

        public string Version { get; set; }
        public string Url { get; set; }
        public string Changelog { get; set; }

        public string Title { get; set; }
        public string Header { get; set; }
        public ICommand OpenReleaseLinkCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        private JsonArray _assets;
        private MainWindowViewModel _mainWindowViewModel;
        private UpdateAvailableDialog _updateDialog;
        public Config Config => _mainWindowViewModel.CurrentConfig;
        public ILogger Log => _mainWindowViewModel.Log;

        public void Initialize(MainWindowViewModel mainWindowViewModel, UpdateAvailableDialog updateDialog, string version, JsonArray assets, string url, string changelog)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _updateDialog = updateDialog;
            Version = version;
            _assets = assets;
            Url = url;
            Changelog = changelog;

            Title = string.Format(Strings.New_Update_Available___0_, Version);
            Header = string.Format(Strings.Serial_Loops_v_0_, Version);
            OpenReleaseLinkCommand = ReactiveCommand.Create(OpenReleaseLink);
            CloseCommand = ReactiveCommand.Create(_updateDialog.Close);
        }

        public void OpenReleaseLink()
        {
            Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
        }
    }
}
