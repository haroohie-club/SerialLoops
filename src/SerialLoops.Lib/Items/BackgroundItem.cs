using System.Collections.Generic;
using System.IO;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items
{
    public class BackgroundItem : Item, IPreviewableGraphic
    {
        public int Id { get; set; }
        public GraphicsFile Graphic1 { get; set; }
        public GraphicsFile Graphic2 { get; set; }
        public BgType BackgroundType { get; set; }
        public string CgName { get; set; }
        public int Flag { get; set; }
        public short ExtrasShort { get; set; }
        public byte ExtrasByte { get; set; }

        public BackgroundItem(string name) : base(name, ItemType.Background)
        {
        }
        public BackgroundItem(string name, int id, BgTableEntry entry, Project project) : base(name, ItemType.Background)
        {
            Id = id;
            BackgroundType = entry.Type;
            Graphic1 = project.Grp.GetFileByIndex(entry.BgIndex1);
            Graphic2 = project.Grp.GetFileByIndex(entry.BgIndex2); // can be null if type is SINGLE_TEX
            CgExtraData cgEntry = project.Extra.Cgs.AsParallel().FirstOrDefault(c => c.BgId == Id);
            if (cgEntry is not null)
            {
                CgName = cgEntry?.Name?.GetSubstitutedString(project);
                Flag = cgEntry?.Flag ?? 0;
                ExtrasShort = cgEntry?.Unknown02 ?? 0;
                ExtrasByte = cgEntry?.Unknown04 ?? 0;
            }
        }

        public override void Refresh(Project project, ILogger log)
        {
        }

        public SKBitmap GetBackground()
        {
            if (BackgroundType == BgType.TEX_CG_SINGLE)
            {
                return Graphic1.GetImage();
            }
            else if (BackgroundType == BgType.TEX_CG_DUAL_SCREEN)
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

        public void SetBackground(SKBitmap image, IProgressTracker tracker, ILogger log)
        {
            PnnQuantizer quantizer = new();
            int transparentIndex = BackgroundType != BgType.TEX_BG ? 0 : -1;
            switch (BackgroundType)
            {
                case BgType.KINETIC_SCREEN:
                    tracker.Focus("Setting screen image...", 1);
                    Graphic2.SetScreenImage(image, quantizer, Graphic1);
                    tracker.Finished++;
                    break;

                case BgType.TEX_CG_SINGLE:
                    tracker.Focus("Setting CG single image...", 1);
                    List<SKColor> singlePalette = Helpers.GetPaletteFromImage(image, transparentIndex == 0 ? 255 : 256, log);
                    if (singlePalette.Count == 255)
                    {
                        singlePalette.Insert(0, new SKColor(0, 248, 0));
                    }
                    Graphic1.SetPalette(singlePalette);
                    Graphic1.SetImage(image);
                    tracker.Finished++;
                    break;

                case BgType.TEX_CG_DUAL_SCREEN:
                    SKBitmap newTextureBitmap = new(Graphic1.Width, Graphic1.Height);
                    SKBitmap newTileBitmap = new(64, Graphic2.Height * Graphic2.Width / 64);
                    SKBitmap tileSource = new(image.Width, image.Height - Graphic1.Height);

                    tracker.Focus("Drawing bottom screen texture...", 1);
                    SKCanvas textureCanvas = new(newTextureBitmap);
                    textureCanvas.DrawBitmap(image, new SKRect(0, image.Height - newTextureBitmap.Height, newTextureBitmap.Width, image.Height), new SKRect(0, 0, newTextureBitmap.Width, newTextureBitmap.Height));
                    textureCanvas.Flush();
                    tracker.Finished++;

                    SKCanvas tileSourceCanvas = new(tileSource);
                    tileSourceCanvas.DrawBitmap(image, 0, 0);
                    tileSourceCanvas.Flush();

                    tracker.Focus("Drawing top screen tiles...", newTileBitmap.Height / 64 * newTileBitmap.Width / 64);
                    SKCanvas tileCanvas = new(newTileBitmap);
                    int currentTile = 0;
                    for (int y = 0; y < tileSource.Height; y += 64)
                    {
                        for (int x = 0; x < tileSource.Width; x += 64)
                        {
                            SKRect crop = new(x, y, x + 64, y + 64);
                            SKRect dest = new(0, currentTile * 64, 64, (currentTile + 1) * 64);
                            tileCanvas.DrawBitmap(tileSource, crop, dest);
                            currentTile++;
                            tracker.Finished++;
                        }
                    }
                    tileCanvas.Flush();

                    tracker.Focus("Setting palettes and images...", 5);
                    List<SKColor> tilePalette = Helpers.GetPaletteFromImage(image, transparentIndex == 0 ? 255 : 256, log);
                    if (tilePalette.Count == 255)
                    {
                        tilePalette.Insert(0, new SKColor(0, 248, 0));
                    }
                    tracker.Finished++;
                    Graphic1.SetPalette(tilePalette);
                    tracker.Finished++;
                    Graphic2.SetPalette(tilePalette);
                    tracker.Finished++;
                    Graphic1.SetImage(newTextureBitmap);
                    tracker.Finished++;
                    Graphic2.SetImage(newTileBitmap);
                    tracker.Finished++;
                    break;

                default:
                    SKBitmap newGraphic1 = new(Graphic1.Width, Graphic1.Height);
                    SKBitmap newGraphic2 = new(Graphic2.Width, Graphic2.Height);

                    tracker.Focus("Drawing textures...", 2);
                    SKCanvas canvas1 = new(newGraphic1);
                    canvas1.DrawBitmap(image, new SKRect(0, 0, newGraphic1.Width, newGraphic1.Height), new SKRect(0, 0, newGraphic1.Width, newGraphic1.Height));
                    canvas1.Flush();
                    tracker.Finished++;

                    SKCanvas canvas2 = new(newGraphic2);
                    canvas2.DrawBitmap(image, new SKRect(0, newGraphic1.Height, newGraphic2.Width, newGraphic1.Height + newGraphic2.Height), new SKRect(0, 0, newGraphic2.Width, newGraphic2.Height));
                    canvas2.Flush();
                    tracker.Finished++;

                    tracker.Focus("Setting palettes and images...", 5);
                    List<SKColor> texPalette = Helpers.GetPaletteFromImage(image, transparentIndex == 0 ? 255 : 256, log);
                    if (texPalette.Count == 255)
                    {
                        texPalette.Insert(0, new SKColor(0, 248, 0));
                    }
                    tracker.Finished++;
                    Graphic1.SetPalette(texPalette);
                    tracker.Finished++;
                    Graphic2.SetPalette(texPalette);
                    tracker.Finished++;
                    Graphic1.SetImage(newGraphic1);
                    tracker.Finished++;
                    Graphic2.SetImage(newGraphic2);
                    tracker.Finished++;
                    break;
            }
        }

        public void Write(Project project, ILogger log)
        {
            using MemoryStream grp1Stream = new();
            Graphic1.GetImage().Encode(grp1Stream, SKEncodedImageFormat.Png, 1);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{Graphic1.Index:X3}.png"), grp1Stream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{Graphic1.Index:X3}.gi"), Graphic1.GetGraphicInfoFile(), project, log);

            if (BackgroundType != BgType.KINETIC_SCREEN && BackgroundType != BgType.TEX_CG_SINGLE)
            {
                using MemoryStream grp2Stream = new();
                Graphic2.GetImage().Encode(grp2Stream, SKEncodedImageFormat.Png, 1);
                IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{Graphic2.Index:X3}.png"), grp2Stream.ToArray(), project, log);
                IO.WriteStringFile(Path.Combine("assets", "graphics", $"{Graphic2.Index:X3}.gi"), Graphic1.GetGraphicInfoFile(), project, log);
            }
            else if (BackgroundType == BgType.KINETIC_SCREEN)
            {
                // TODO: Export screen information for KBGs
            }
        }

        public SKBitmap GetPreview(Project project)
        {
            return GetBackground();
        }
    }
}
