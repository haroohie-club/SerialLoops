using HaruhiChokuretsuLib.Archive.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Items
{
    public class ScenarioItem : Item
    {
        public ScenarioStruct Scenario { get; set; }

        public ScenarioItem(ScenarioStruct scenario) : base("Scenario", ItemType.Scenario)
        {
            Scenario = scenario;
        }

        public override void Refresh(Project project)
        {
        }
    }
}
