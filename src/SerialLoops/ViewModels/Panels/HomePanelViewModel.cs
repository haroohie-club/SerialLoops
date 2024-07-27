using HaruhiChokuretsuLib.Util;

namespace SerialLoops.ViewModels.Panels
{
    public partial class HomePanelViewModel : ViewModelBase
    {
        public MainWindowViewModel MainWindow { get; set; }
        public ILogger Log => MainWindow.Log;

        public void Initialize(MainWindowViewModel mainWindow)
        {
            MainWindow = mainWindow;
        }
    }
}
