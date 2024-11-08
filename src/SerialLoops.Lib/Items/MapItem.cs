using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items;

public class MapItem : Item
{
    public MapFile Map { get; set; }
    public int QmapIndex { get; set; }

    public MapItem(string name) : base(name, ItemType.Map)
    {
    }
    public MapItem(MapFile map, int qmapIndex, Project project) : base(map.Name[0..^1], ItemType.Map)
    {
        Map = map;
        QmapIndex = qmapIndex;
    }

    public SKPoint GetOrigin(ArchiveFile<GraphicsFile> grp)
    {
        GraphicsFile layout = grp.GetFileByIndex(Map.Settings.LayoutFileIndex);
        return new SKPoint(layout.LayoutEntries[Map.Settings.LayoutSizeDefinitionIndex].ScreenX, layout.LayoutEntries[Map.Settings.LayoutSizeDefinitionIndex].ScreenY);
    }

    public SKBitmap GetMapImage(ArchiveFile<GraphicsFile> grp, bool displayPathingMap, bool displayMapStart)
    {
        SKBitmap map;
        if (Map.Settings.BackgroundLayoutStartIndex > 0)
        {
            map = Map.GetMapImages(grp, 0, Map.Settings.BackgroundLayoutStartIndex);
        }
        else
        {
            map = Map.GetMapImages(grp, 0, grp.GetFileByIndex(Map.Settings.LayoutFileIndex).LayoutEntries.Count);
        }
        SKBitmap mapWithGrid = new(map.Width, map.Height);
        SKCanvas canvas = new(mapWithGrid);
        canvas.DrawBitmap(map, new SKPoint(0, 0));

        SKPoint gridZero = GetOrigin(grp);
        if (displayPathingMap)
        {
            if (Map.Settings.SlgMode)
            {
                for (int y = 0; y < Map.PathingMap.Length; y++)
                {
                    for (int x = 0; x < Map.PathingMap[y].Length; x++)
                    {
                        SKPoint origin = new(gridZero.X - x * 32 + y * 32, gridZero.Y + x * 16 + y * 16);
                        SKPath diamond = new();
                        diamond.AddPoly(
                        [
                            origin,
                            new(origin.X - 32, origin.Y + 16),
                            new(origin.X, origin.Y + 32),
                            new(origin.X + 32, origin.Y + 16)
                        ]);
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
                        diamond.AddPoly(
                        [
                            origin,
                            new(origin.X - 16, origin.Y + 8),
                            new(origin.X, origin.Y + 16),
                            new(origin.X + 16, origin.Y + 8),
                        ]);
                        canvas.DrawRegion(new SKRegion(diamond), GetPathingCellPaint(x, y));
                    }
                }
            }
        }

        if (displayMapStart)
        {
            SKBitmap icon;
            SKPoint start;
            if (Map.Settings.SlgMode)
            {
                start = new(gridZero.X - Map.Settings.StartingPosition.x * 32 + Map.Settings.StartingPosition.y * 32, gridZero.Y + Map.Settings.StartingPosition.x * 16 + Map.Settings.StartingPosition.y * 16 + 16);
                icon = GetMapIcon("Origin_Point", 32);
            }
            else
            {
                start = new(gridZero.X - Map.Settings.StartingPosition.x * 16 + Map.Settings.StartingPosition.y * 16, gridZero.Y + Map.Settings.StartingPosition.x * 8 + Map.Settings.StartingPosition.y * 8 + 8);
                icon = GetMapIcon("Origin_Point", 16);
            }
            canvas.DrawBitmap(icon, start);
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

    private static SKBitmap GetMapIcon(string name, int size)
    {
        SKBitmap icon = new(size, size);
        SKImage image = SKImage.FromEncodedData($"MapIcons/{name}.png");
        SKBitmap.FromImage(image).ScalePixels(icon, SKFilterQuality.Medium);
        return icon;
    }

    public override void Refresh(Project project, ILogger log)
    {
    }
}