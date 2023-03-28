using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using SerialLoops.Lib.Util;
using SkiaSharp;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class BackgroundItem : Item, IPreviewableGraphic
    {
        public int Id { get; set; }
        public GraphicsFile Graphic1 { get; set; }
        public GraphicsFile Graphic2 { get; set; }
        public BgType BackgroundType { get; set; }
        public string CgName { get; set; }
        public short ExtrasShort { get; set; }
        public int ExtrasInt { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public BackgroundItem(string name) : base(name, ItemType.Background)
        {
        }
        public BackgroundItem(string name, int id, BgTableEntry entry, Project project) : base(name, ItemType.Background)
        {
            Id = id;
            BackgroundType = entry.Type;
            Graphic1 = project.Grp.Files.First(g => g.Index == entry.BgIndex1);
            Graphic2 = project.Grp.Files.FirstOrDefault(g => g.Index == entry.BgIndex2); // can be null if type is SINGLE_TEX
            CgStruct cgEntry = project.Extra.Cgs.FirstOrDefault(c => c.BgId == Id);
            if (cgEntry is not null)
            {
                CgName = cgEntry?.Name?.GetSubstitutedString(project);
                ExtrasShort = cgEntry?.Unknown02 ?? 0;
                ExtrasInt = cgEntry?.Unknown04 ?? 0;
            }
            PopulateScriptUses(project.Evt);
        }

        public override void Refresh(Project project)
        {
            PopulateScriptUses(project.Evt);
        }

        public SKBitmap GetBackground()
        {
            if (BackgroundType == BgType.SINGLE_TEX)
            {
                return Graphic1.GetImage();
            }
            else if (BackgroundType == BgType.TEX_DUAL)
            {
                SKBitmap bitmap = new(Graphic1.Width, Graphic1.Height + Graphic2.Height);
                SKCanvas canvas = new(bitmap);

                SKBitmap tileBitmap = new(Graphic2.Width, Graphic2.Height);
                SKBitmap tiles = Graphic2.GetImage(width: 64);
                SKCanvas tileCanvas = new(tileBitmap);
                int currentTile = 0;
                for (int y = 0; y < tileBitmap.Height; y += 64)
                {
                    for (int x = 0; x < tileBitmap.Width; x += 64)
                    {
                        SKRect crop = new(0, currentTile * 64, 64, (currentTile + 1) * 64);
                        SKRect dest = new(x, y, x + 64, y + 64);
                        tileCanvas.DrawBitmap(tiles, crop, dest);
                        currentTile++;
                    }
                }
                tileCanvas.Flush();

                canvas.DrawBitmap(tileBitmap, new SKPoint(0, 0));
                canvas.DrawBitmap(Graphic1.GetImage(), new SKPoint(0, Graphic2.Height));
                canvas.Flush();

                return bitmap;
            }
            else if (BackgroundType == BgType.KINETIC_SCREEN)
            {
                return Graphic2.GetScreenImage(Graphic1);
            }
            else
            {
                SKBitmap bitmap = new(Graphic1.Width, Graphic1.Height + Graphic2.Height);
                SKCanvas canvas = new(bitmap);

                canvas.DrawBitmap(Graphic1.GetImage(), new SKPoint(0, 0));
                canvas.DrawBitmap(Graphic2.GetImage(), new SKPoint(0, Graphic1.Height));
                canvas.Flush();

                return bitmap;
            }
        }

        public void PopulateScriptUses(ArchiveFile<EventFile> evt)
        {
            string[] bgCommands = new string[] { "KBG_DISP", "BG_DISP", "BG_DISP2", "BG_DISPTEMP", "BG_FADE" };

            ScriptUses = evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => bgCommands.Contains(c.Command.Mnemonic)).Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[0] == Id || t.c.Command.Mnemonic == "BG_FADE" && t.c.Parameters[1] == Id).ToArray();
        }

        public SKBitmap GetPreview(Project project)
        {
            return GetBackground();
        }
    }
}
