using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Event;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class AddScenarioCommandDialogViewModel : ViewModelBase
{
    public ObservableCollection<ScenarioCommand.ScenarioVerb> Verbs => new(Enum.GetValues<ScenarioCommand.ScenarioVerb>());
    [Reactive]
    public ScenarioCommand.ScenarioVerb SelectedVerb { get; set; }

    public ICommand CreateCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    public AddScenarioCommandDialogViewModel()
    {
        CreateCommand = ReactiveCommand.Create<AddScenarioCommandDialog>((dialog) => dialog.Close(SelectedVerb));
        CancelCommand = ReactiveCommand.Create<AddScenarioCommandDialog>((dialog) => dialog.Close());
    }
}
