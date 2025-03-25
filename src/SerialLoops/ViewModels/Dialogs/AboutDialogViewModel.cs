using System.Windows.Input;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class AboutDialogViewModel : ViewModelBase
{
    public string Version => string.Format(Strings.AboutDialogVersionString, Shared.GetVersion());

    public ICommand CloseCommand { get; } = ReactiveCommand.Create<AboutDialog>(dialog => dialog.Close());
}
