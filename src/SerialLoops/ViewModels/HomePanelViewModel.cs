using HaruhiChokuretsuLib.Util;

namespace SerialLoops.ViewModels
{
    public partial class HomePanelViewModel : ViewModelBase
    {
        private ILogger _log;

        public HomePanelViewModel(ILogger log)
        {
            _log = log;
        }
    }
}
