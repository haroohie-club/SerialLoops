using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Logging;
using System;

namespace SerialLoops.Dialogs
{
    public abstract class FindItemsWindow : FloatingForm
    {
        public ILogger Log { get; set; }
        public EditorTabsPanel Tabs { get; set; }
        public ItemExplorerPanel Explorer { get; set; }
        public Project Project { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            if (Log is null)
            {
                // We can't log that log is null, so we have to throw
                throw new LoggerNullException();
            }
            if (Project is null)
            {
                Log.LogError($"Project not provided to project creation dialog");
                Close();
            }
            if (Tabs is null)
            {
                Log.LogError($"Editor Tabs not provided to project creation dialog");
                Close();
            }
            base.OnLoad(e);
        }
    }
}
