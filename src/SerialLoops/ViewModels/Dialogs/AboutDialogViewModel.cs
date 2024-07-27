using System.Reflection;

namespace SerialLoops.ViewModels.Dialogs
{
    public class AboutDialogViewModel : ViewModelBase
    {
        public string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        public int Width => 500;
        public int Height => 450;
        public int GridWidth => Width - 50;
    }
}
