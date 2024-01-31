using HaruhiChokuretsuLib.Util;
using SerialLoops.Dialogs;

namespace SerialLoops
{
    public partial class SearchDialog : FindItemsDialog
    {
        public SearchDialog(ILogger log)
        {
            Log = log;
            InitializeComponent();
        }
    }
}
