using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items;

public class MapItem : Item
{
    public MapFile Map { get; set; }
    public int QmapIndex { get; set; }

    public GraphicsFile Layout { get; }
    public SKBitmap BgBitmap { get; set; }
    public SKBitmap BgObjBitmap { get; set; }
    public SKBitmap ObjBitmap { get; set; }

    public MapItem(MapFile map, int qmapIndex, Project project) : base(map.Name[..^1], ItemType.Map)
    {
        Map = map;
        QmapIndex = qmapIndex;
        Layout = project.Grp.GetFileByIndex(Map.Settings.LayoutFileIndex);
        BgBitmap = project.Grp.GetFileByIndex(Map.Settings.TextureFileIndices[0]).GetImage();
        BgObjBitmap = project.Grp.GetFileByIndex(Map.Settings.TextureFileIndices[1]).GetImage();
        ObjBitmap = project.Grp.GetFileByIndex(Map.Settings.TextureFileIndices[2]).GetImage();
    }

    public SKPoint GetOrigin(ArchiveFile<GraphicsFile> grp)
    {
        GraphicsFile layout = grp.GetFileByIndex(Map.Settings.LayoutFileIndex);
        return new(layout.LayoutEntries[Map.Settings.LayoutBgLayerStartIndex].ScreenX, layout.LayoutEntries[Map.Settings.LayoutBgLayerStartIndex].ScreenY);
    }

    public SKBitmap GetMapImage(ArchiveFile<GraphicsFile> grp, bool displayPathingMap, bool displayMapStart)
    {
        SKBitmap map;
        if (Map.Settings.ScrollingBgDefinitionLayoutIndex > 0)
        {
            map = Map.GetMapImages(grp, 0, Map.Settings.ScrollingBgLayoutStartIndex);
        }
        else
        {
            map = Map.GetMapImages(grp, 0, grp.GetFileByIndex(Map.Settings.LayoutFileIndex).LayoutEntries.Count);
        }
        SKBitmap mapWithGrid = new(map.Width, map.Height);
        using SKCanvas canvas = new(mapWithGrid);
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
                        canvas.DrawRegion(new(diamond), GetPathingCellPaint(x, y));
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
                        canvas.DrawRegion(new(diamond), GetPathingCellPaint(x, y));
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

    public SKPoint GetPositionFromGrid(int x, int y, SKPoint origin)
    {
        return GetPositionFromGrid(x, y, origin, Map.Settings.SlgMode);
    }

    public static SKPoint GetPositionFromGrid(int x, int y, SKPoint origin, bool slgMode)
    {
        return slgMode
            ? new(origin.X - x * 32 + y * 32, origin.Y + x * 16 + y * 16)
            : new(origin.X - y * 16 + x * 16, origin.Y + y * 8 + x * 8);
    }

    private SKPaint GetPathingCellPaint(int x, int y)
    {
        return Map.PathingMap[x][y] switch
        {
            1 => new() { Color = new(0, 128, 0, 186) }, // walkable
            2 => new() { Color = new(0, 200, 200, 186) }, // spawnable
            _ => new() { Color = new(255, 0, 0, 186) }, // unwalkable
        };
    }

    private static SKBitmap GetMapIcon(string name, int size)
    {
        SKBitmap icon = new(size, size);
        SKImage image = SKImage.FromEncodedData($"MapIcons/{name}.png");
        SKBitmap.FromImage(image).ScalePixels(icon, Extensions.HighQualitySamplingOptions);
        return icon;
    }

    public override void Refresh(Project project, ILogger log)
    {
    }

    public override string ToString() => DisplayName;
}
