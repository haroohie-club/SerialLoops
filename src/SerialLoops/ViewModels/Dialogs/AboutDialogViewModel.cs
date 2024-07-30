using System.Reflection;

namespace SerialLoops.ViewModels.Dialogs
{
    public class AboutDialogViewModel : ViewModelBase
    {
        public string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        public double Width => 500;
        public double Height => 450;
    }
}
