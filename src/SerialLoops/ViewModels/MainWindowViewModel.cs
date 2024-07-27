using Avalonia;
using SerialLoops.Lib;
using SerialLoops.Utility;

namespace SerialLoops.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Title { get; set; } = "Serial Loops";
        public Size MinSize => new(769, 420);
        public Size ClientSize { get; set; } = new(1200, 800);

        public ProjectsCache ProjectsCache { get; set; }
        public Config CurrentConfig { get; set; }
        public Project OpenProject { get; set; }
       

        public LoopyLogger Log { get; set; }
    }
}
