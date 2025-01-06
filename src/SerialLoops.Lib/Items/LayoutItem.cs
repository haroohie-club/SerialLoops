using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items;

public class LayoutItem : Item, IPreviewableGraphic
{
    public GraphicsFile Layout { get; set; }
    public List<GraphicsFile> GraphicsFiles { get; set; }
    public int StartEntry { get; set; }
    public int NumEntries { get; set; }

    public Dictionary<int, SKBitmap> TilesDict { get; }

    public LayoutItem(int layoutIndex, List<GraphicsFile> grps, int startEntry, int numEntries, string name, Project project) : base(name, ItemType.Layout)
    {
        Layout = project.LayoutFiles[layoutIndex];
        GraphicsFiles = grps;
        StartEntry = startEntry;
        NumEntries = numEntries;
        TilesDict = grps.Select((g, i) => (i, g.GetImage(transparentIndex: 0))).ToDictionary();
    }

    public LayoutItem(GraphicsFile layout, List<GraphicsFile> grps, int startEntry, int numEntries, string name) : base(name, ItemType.Layout)
    {
        Layout = layout;
        GraphicsFiles = grps;
        StartEntry = startEntry;
        NumEntries = numEntries;
        TilesDict = grps.Select((g, i) => (i, g.GetImage(transparentIndex: 0))).ToDictionary();
    }

    public override void Refresh(Project project, ILogger log)
    {
    }

    public SKBitmap GetLayoutImage()
    {
        return Layout.GetLayout(GraphicsFiles, Layout.LayoutEntries.Skip(StartEntry).Take(NumEntries).ToList(), darkMode: false, preprocessedList: true).bitmap;
    }

    public SKBitmap GetLayoutEntryRender(int index)
    {
        if (index < 0 || Layout.LayoutEntries[index].RelativeShtxIndex < 0)
        {
            return null;
        }

        return Layout.LayoutEntries[index].GetTileBitmap(TilesDict);
    }

    SKBitmap IPreviewableGraphic.GetPreview(Project project)
    {
        return GetLayoutImage();
    }
}
