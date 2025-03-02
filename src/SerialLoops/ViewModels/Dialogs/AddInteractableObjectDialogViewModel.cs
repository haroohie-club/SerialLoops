using System.Collections.ObjectModel;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Data;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.ViewModels.Editors;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class AddInteractableObjectDialogViewModel : ViewModelBase
{
    public ObservableCollection<ReactiveInteractableObject> UnusedInteractableObjects { get; }
    [Reactive]
    public ReactiveInteractableObject SelectedInteractableObject { get; set; }

    public ICommand AddCommand { get; }
    public ICommand CancelCommand { get; }

    public AddInteractableObjectDialogViewModel(ObservableCollection<ReactiveInteractableObject> unusedInteractableObjects)
    {
        UnusedInteractableObjects = unusedInteractableObjects;

        AddCommand = ReactiveCommand.Create<AddInteractableObjectDialog>(dialog => dialog.Close(SelectedInteractableObject));
        CancelCommand = ReactiveCommand.Create<AddInteractableObjectDialog>(dialog => dialog.Close());
    }
}
