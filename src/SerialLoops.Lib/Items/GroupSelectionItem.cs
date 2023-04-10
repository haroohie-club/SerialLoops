using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items
{
    public class GroupSelectionItem : Item
    {
        public ScenarioSelectionStruct Selection { get; set; }
        public int Index { get; set; }

        public GroupSelectionItem(ScenarioSelectionStruct selection, int index, Project project) : base($"Selection {index}", ItemType.Group_Selection)
        {
            Selection = selection;
            Index = index;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }
    }
}
