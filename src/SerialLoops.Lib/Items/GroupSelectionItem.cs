using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items;

public class GroupSelectionItem : Item
{
    public ScenarioSelection Selection { get; set; }
    public int Index { get; set; }

    public GroupSelectionItem(ScenarioSelection selection, int index, Project project) : base($"Selection {index + 1}", ItemType.Group_Selection)
    {
        Selection = selection;
        Index = index;
    }

    public override void Refresh(Project project, ILogger log)
    {
    }
}