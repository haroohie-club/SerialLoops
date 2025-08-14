using System.Windows.Input;
using AvaloniaEdit.Document;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class LogViewerDialogViewModel : ViewModelBase
{
    public string Title { get; set; }
    public TextDocument Log { get; set; }

    public ICommand CloseCommand { get; }

    public LogViewerDialogViewModel(string logName, string log)
    {
        Title = string.Format(Strings.LogViewerTitleFormatString, logName);
        Log = new(log);

        CloseCommand = ReactiveCommand.Create<LogViewerDialog>(dialog => dialog.Close());
    }
}
