using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Editors
{
    public class ScenarioEditor : Editor
    {
        private ScenarioItem _scenario;

        public ScenarioEditor(ScenarioItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            _scenario = (ScenarioItem)Description;
            StackLayout mainLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20,
            };

            return mainLayout;
        }
    }
}
