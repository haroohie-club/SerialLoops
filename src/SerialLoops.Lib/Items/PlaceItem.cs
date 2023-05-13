using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class PlaceItem : Item, IPreviewableGraphic
    {
        public int Index { get; set; }
        public GraphicsFile PlaceGraphic { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public PlaceItem(int index, GraphicsFile placeGrp, Project project) : base(placeGrp.Name[0..^3], ItemType.Place)
        {
            Index = index;
            CanRename = false;
            PlaceGraphic = placeGrp;
            PopulateScriptUses(project.Evt);
        }

        public override void Refresh(Project project, ILogger log)
        {
            PopulateScriptUses(project.Evt);
        }

        public SKBitmap GetPreview(Project project)
        {
            SKBitmap placeGraphic = PlaceGraphic.GetImage(transparentIndex: 0);
            SKBitmap adjustedPlace = new(placeGraphic.Width, placeGraphic.Height);
            SKCanvas canvas = new(adjustedPlace);

            int col = 0, row = 0;
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    canvas.DrawBitmap(placeGraphic,
                        new SKRect(x * placeGraphic.Width / 2, y * placeGraphic.Height / 4, (x + 1) * placeGraphic.Width / 2, (y + 1) * placeGraphic.Height / 4),
                        new SKRect(col * placeGraphic.Width / 2, row * placeGraphic.Height / 4, (col + 1) * placeGraphic.Width / 2, (row + 1) * placeGraphic.Height / 4));
                    row++;
                    if (row >= 4)
                    {
                        row = 0;
                        col++;
                    }
                }
            }

            canvas.Flush();

            return adjustedPlace;
        }

        public void PopulateScriptUses(ArchiveFile<EventFile> evt)
        {
            ScriptUses = evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => c.Command.Mnemonic == "SET_PLACE").Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[1] == Index).ToArray();
        }
    }
}
