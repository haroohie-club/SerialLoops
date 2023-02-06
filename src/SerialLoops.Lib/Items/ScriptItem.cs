using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Items
{
    public class ScriptItem : Item
    {
        public EventFile Event { get; set; }

        public ScriptItem(string name) : base(name, ItemType.Script)
        {
        }
        public ScriptItem(EventFile evt) : base(evt.Name[0..^1], ItemType.Script)
        {
            Event = evt;
        }

        public override void Refresh(Project project)
        {
        }
    }
}
