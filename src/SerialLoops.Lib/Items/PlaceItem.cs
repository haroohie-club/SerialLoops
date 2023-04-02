using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Data;

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
            PlaceGraphic = placeGrp;
            PopulateScriptUses(project.Evt);
        }

        public override void Refresh(Project project)
        {
            PopulateScriptUses(project.Evt);
        }

        public SKBitmap GetPreview(Project project)
        {
            SKBitmap placeGraphic = PlaceGraphic.GetImage(transparentIndex: 0);
            SKBitmap adjustedPlace = new(placeGraphic.Width, placeGraphic.Height / 2);
            SKCanvas canvas = new(adjustedPlace);

            canvas.DrawBitmap(placeGraphic,
                new SKRect(placeGraphic.Width / 2, 0, placeGraphic.Width, placeGraphic.Height / 4),
                new SKRect(0, 0, adjustedPlace.Width / 2, adjustedPlace.Height / 2));
            canvas.DrawBitmap(placeGraphic,
                new SKRect(0, placeGraphic.Height / 4, placeGraphic.Width / 2, placeGraphic.Height / 2),
                new SKRect(0, adjustedPlace.Height / 2, adjustedPlace.Width / 2, adjustedPlace.Height));
            canvas.DrawBitmap(placeGraphic,
                new SKRect(placeGraphic.Width / 2, placeGraphic.Height / 2, placeGraphic.Width, (int)(3.0 / 4 * placeGraphic.Height)),
                new SKRect(adjustedPlace.Width / 2, 0, adjustedPlace.Width, adjustedPlace.Height / 2));
            canvas.DrawBitmap(placeGraphic,
                new SKRect(0, (int)(3.0 / 4 * placeGraphic.Height), placeGraphic.Width / 2, placeGraphic.Height),
                new SKRect(adjustedPlace.Width / 2, adjustedPlace.Height / 2, adjustedPlace.Width, adjustedPlace.Height));
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
