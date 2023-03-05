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

        public SKPoint GetOrigin(ArchiveFile<GraphicsFile> grp)
        {
            GraphicsFile layout = grp.Files.First(f => f.Index == Map.Settings.LayoutFileIndex);
            return new SKPoint(layout.LayoutEntries[Map.Settings.LayoutSizeDefinitionIndex].ScreenX, layout.LayoutEntries[Map.Settings.LayoutSizeDefinitionIndex].ScreenY);
        }

        public SKBitmap GetMapImage(ArchiveFile<GraphicsFile> grp, bool displayPathingMap)
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

            if (displayPathingMap)
            {
                SKPoint gridZero = GetOrigin(grp);

                if (Map.Settings.SlgMode)
                {
                    for (int y = 0; y < Map.PathingMap.Length; y++)
                    {
                        for (int x = 0; x < Map.PathingMap[y].Length; x++)
                        {
                            SKPoint origin = new(gridZero.X - x * 32 + y * 32, gridZero.Y + x * 16 + y * 16);
                            SKPath diamond = new();
                            diamond.AddPoly(new SKPoint[]
                            {
                                origin,
                                new SKPoint(origin.X - 32, origin.Y + 16),
                                new SKPoint(origin.X, origin.Y + 32),
                                new SKPoint(origin.X + 32, origin.Y + 16)
                            });
                            canvas.DrawRegion(new SKRegion(diamond), GetPathingCellPaint(x, y));
                        }
                    }
                }
                else
                {
                    for (int x = 0; x < Map.PathingMap.Length; x++)
                    {
                        for (int y = 0; y < Map.PathingMap[x].Length; y++)
                        {
                            SKPoint origin = new(gridZero.X - x * 16 + y * 16, gridZero.Y + x * 8 + y * 8);
                            SKPath diamond = new();
                            diamond.AddPoly(new SKPoint[]
                            {
                                origin,
                                new SKPoint(origin.X - 16, origin.Y + 8),
                                new SKPoint(origin.X, origin.Y + 16),
                                new SKPoint(origin.X + 16, origin.Y + 8),
                            });
                            canvas.DrawRegion(new SKRegion(diamond), GetPathingCellPaint(x, y));
                        }
                    }
                }
            }

            canvas.Flush();
            return mapWithGrid;
        }

        private SKPaint GetPathingCellPaint(int x, int y)
        {
            return Map.PathingMap[x][y] switch
            {
                1 => new() { Color = new SKColor(0, 128, 0, 186) }, // walkable
                2 => new() { Color = new SKColor(0, 200, 200, 186) }, // spawnable
                _ => new() { Color = new SKColor(255, 0, 0, 186) }, // unwalkable
            };
        }

        public override void Refresh(Project project)
        {
        }
    }
}
