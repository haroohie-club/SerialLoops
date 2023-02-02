using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;

namespace SerialLoops.Lib.Items
{
    public abstract class Item : ItemDescription
    {

        public Item(string name, ItemType type) : base(name, type)
        {
        }

        public abstract void Refresh(ArchiveFile<DataFile> dat, ArchiveFile<EventFile> evt, ArchiveFile<GraphicsFile> grp);
    }

}