using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Items
{
    public class MapItem : Item
    {
        public MapFile Map { get; set; }
        public MapItem(string name) : base(name, ItemType.Map)
        {
        }
        public MapItem(MapFile map) : base(map.Name[0..^1], ItemType.Map)
        {
            Map = map;
        }

        public override void Refresh(ArchiveFile<DataFile> dat, ArchiveFile<EventFile> evt, ArchiveFile<GraphicsFile> grp)
        {
            throw new NotImplementedException();
        }
    }
}
