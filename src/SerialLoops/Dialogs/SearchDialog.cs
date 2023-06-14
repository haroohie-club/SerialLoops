using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;

namespace SerialLoops
{
    public partial class SearchDialog : FindItemsWindow
    {
        public SearchDialog(ILogger log)
        {
            Log = log;
            InitializeComponent();
        }
    }
}
