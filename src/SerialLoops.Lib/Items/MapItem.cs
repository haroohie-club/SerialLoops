using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class MapItem : Item
    {
        public MapFile Map { get; set; }
        public int QmapIndex { get; set; }

        public MapItem(string name) : base(name, ItemType.Map)
        {
        }
        public MapItem(MapFile map, int qmapIndex) : base(map.Name[0..^1], ItemType.Map)
        {
            Map = map;
            QmapIndex = qmapIndex;
        }

        public SKBitmap GetMapWithGrid(ArchiveFile<GraphicsFile> grp)
        {
            SKBitmap map;
            if (Map.Settings.BackgroundLayoutStartIndex > 0)
            {
                map = Map.GetMapImages(grp, 0, Map.Settings.BackgroundLayoutStartIndex);
            }
            else
            {
                map = Map.GetMapImages(grp, 0, grp.Files.First(f => f.Index == Map.Settings.LayoutFileIndex).LayoutEntries.Count);
            }
            SKBitmap mapWithGrid = new(map.Width, map.Height);
            SKCanvas canvas = new(mapWithGrid);
            canvas.DrawBitmap(map, new SKPoint(0, 0));

            canvas.Flush();
            return mapWithGrid;
        }

        public override void Refresh(Project project)
        {
        }
    }
}
