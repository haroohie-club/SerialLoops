using System.Reflection;
using System.Windows.Input;
using ReactiveUI;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class AboutDialogViewModel : ViewModelBase
{
    public string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

    public ICommand CloseCommand { get; } = ReactiveCommand.Create<AboutDialog>(dialog => dialog.Close());
}
