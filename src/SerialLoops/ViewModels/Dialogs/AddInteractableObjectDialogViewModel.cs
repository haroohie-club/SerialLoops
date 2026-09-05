using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SerialLoops.ViewModels.Editors;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public partial class AddInteractableObjectDialogViewModel : ViewModelBase
{
    public ObservableCollection<ReactiveInteractableObject> UnusedInteractableObjects { get; }
    [Reactive]
    public partial ReactiveInteractableObject SelectedInteractableObject { get; set; }
    public bool HackApplied { get; }

    public ICommand AddCommand { get; }
    public ICommand CancelCommand { get; }

    public AddInteractableObjectDialogViewModel(ObservableCollection<ReactiveInteractableObject> unusedInteractableObjects, bool hackApplied)
    {
        UnusedInteractableObjects = unusedInteractableObjects;
        HackApplied = hackApplied;

        AddCommand = ReactiveCommand.Create<AddInteractableObjectDialog>(dialog => dialog.Close(SelectedInteractableObject));
        CancelCommand = ReactiveCommand.Create<AddInteractableObjectDialog>(dialog => dialog.Close());
    }
}
