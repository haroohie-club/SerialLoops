using System.Diagnostics;
using System.Drawing;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class UpdateAvailableDialogViewModel : ViewModelBase
{
    public string Version { get; }
    public string Url { get; }

    [Reactive]
    public string Changelog { get; set; }

    public string Title { get; set; }
    public string Header { get; set; }
    public ICommand OpenReleaseLinkCommand { get; set; }
    public ICommand CloseCommand { get; set; }
    private Config _config { get; }
    private ILogger _log;

    [Reactive]
    public bool CheckForUpdates { get; set; }
    [Reactive]
    public bool UsePreReleaseChannel { get; set; }

    public UpdateAvailableDialogViewModel(MainWindowViewModel mainWindowViewModel, string version, string url, string changelog)
    {
        _log = mainWindowViewModel.Log;
        Version = version;
        Url = url;
        Changelog = changelog;

        Title = string.Format(Strings.New_Update_Available___0_, Version);
        Header = string.Format(Strings.Serial_Loops_v_0_, Version);
        _config = mainWindowViewModel.CurrentConfig;
        CheckForUpdates = _config.CheckForUpdates;
        UsePreReleaseChannel = _config.PreReleaseChannel;

        OpenReleaseLinkCommand = ReactiveCommand.Create(OpenReleaseLink);
        CloseCommand = ReactiveCommand.Create<UpdateAvailableDialog>((dialog) =>
        {
            _config.CheckForUpdates = CheckForUpdates;
            _config.PreReleaseChannel = UsePreReleaseChannel;
            _config.Save(_log);
            dialog.Close();
        });
    }

    private void OpenReleaseLink()
    {
        Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
    }
}
