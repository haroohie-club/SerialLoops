using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class LayoutItem(GraphicsFile layoutFile, List<GraphicsFile> grps, int startEntry, int numEntries, string name) : Item(name, ItemType.Layout), IPreviewableGraphic
    {
        public GraphicsFile Layout { get; set; } = layoutFile;
        public List<GraphicsFile> GraphicsFiles { get; set; } = grps;
        public int StartEntry { get; set; } = startEntry;
        public int NumEntries { get; set; } = numEntries;

        private readonly Dictionary<int, SKBitmap> _tilesDict = grps.Select((g, i) => (i, g.GetImage(transparentIndex: 0))).ToDictionary();

        public override void Refresh(Project project, ILogger log)
        {
        }

        public SKBitmap GetLayoutImage()
        {
            return Layout.GetLayout(GraphicsFiles, Layout.LayoutEntries.Skip(StartEntry).Take(NumEntries).ToList(), darkMode: false, preprocessedList: true).bitmap;
        }

        public (SKBitmap tile, SKRect dest) GetLayoutEntryRender(int index)
        {
            return (Layout.LayoutEntries[index].GetTileBitmap(_tilesDict), Layout.LayoutEntries[index].GetDestination()); 
        }

        public void Write(Project project, ILogger log)
        {
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{Layout.Index:X3}.lay"), Layout.GetBytes(), project, log);
        }

        SKBitmap IPreviewableGraphic.GetPreview(Project project)
        {
            return GetLayoutImage();
        }
    }
}
