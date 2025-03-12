using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Event;
using ReactiveUI;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class AddScriptCommandDialogViewModel : ViewModelBase
{
    public ObservableCollection<string> CommandVerbs { get; } = new(Enum.GetNames<EventFile.CommandVerb>().OrderBy(c => c));
    private EventFile.CommandVerb _selectedCommandVerb = EventFile.CommandVerb.DIALOGUE;
    public string SelectedCommandVerb
    {
        get => _selectedCommandVerb.ToString();
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCommandVerb, Enum.Parse<EventFile.CommandVerb>(value));
        }
    }

    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    public AddScriptCommandDialogViewModel()
    {
        CreateCommand = ReactiveCommand.Create<AddScriptCommandDialog>((dialog) => dialog.Close(_selectedCommandVerb));
        CancelCommand = ReactiveCommand.Create<AddScriptCommandDialog>((dialog) => dialog.Close());
    }
}
