using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items
{
    public class TutorialItem : Item
    {
        public Tutorial Tutorial { get; set; }

        public TutorialItem(Tutorial tutorial) : base($"TUTORIAL_{tutorial.Id - 1}", ItemType.Tutorial)
        {
            Tutorial = tutorial;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }
    }
}
