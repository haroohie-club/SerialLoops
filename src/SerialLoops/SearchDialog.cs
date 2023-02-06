using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.Generic;
using HaruhiChokuretsuLib.Util;

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
