using System.Windows.Input;
using ReactiveUI;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class AddScriptSectionDialogViewModel : ViewModelBase
{
    public string SectionName { get; set; }

    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    public AddScriptSectionDialogViewModel()
    {
        CreateCommand = ReactiveCommand.Create<AddScriptSectionDialog>((dialog) => dialog.Close(SectionName));
        CancelCommand = ReactiveCommand.Create<AddScriptSectionDialog>((dialog) => dialog.Close());
    }
}
