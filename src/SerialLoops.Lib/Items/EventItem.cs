using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Items
{
    public class EventItem : Item
    {
        public EventFile Event { get; set; }
        public EventItem(string name) : base(name, ItemType.Event)
        {
        }
        public EventItem(EventFile evt) : base(evt.Name[0..^1], ItemType.Event)
        {
            Event = evt;
        }

    }
}
