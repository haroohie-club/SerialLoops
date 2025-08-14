using System.Windows.Input;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class LogViewerDialogViewModel : ViewModelBase
{
    public string Title { get; set; }
    public string Log { get; set; }

    public ICommand CloseCommand { get; }

    public LogViewerDialogViewModel(string logName, string log)
    {
        Title = string.Format(Strings.LogViewerTitleFormatString, logName);
        Log = log;

        CloseCommand = ReactiveCommand.Create<LogViewerDialog>(dialog => dialog.Close());
    }
}
