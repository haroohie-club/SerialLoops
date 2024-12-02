using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Script;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class ScriptTemplateSelectorDialogViewModel : ViewModelBase
{
    public string Filter { get; set; } = string.Empty;
    public ObservableCollection<ScriptTemplate> ScriptTemplates { get; }
    [Reactive]
    public ScriptTemplate SelectedScriptTemplate { get; set; }

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    public ScriptTemplateSelectorDialogViewModel(Project project)
    {
        ScriptTemplates = project.Config.ScriptTemplates;
        ConfirmCommand = ReactiveCommand.Create<ScriptTemplateSelectorDialog>((dialog) => dialog.Close(SelectedScriptTemplate));
        CancelCommand = ReactiveCommand.Create<ScriptTemplateSelectorDialog>((dialog) => dialog.Close());
    }
}
