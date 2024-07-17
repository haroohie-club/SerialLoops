using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;
using System.IO;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class SystemTextureItem : Item
    {
        public SystemTexture SysTex { get; set; }
        public GraphicsFile Grp { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public SystemTextureItem(SystemTexture sysTex, Project project, string name, int width = -1, int height = -1) : base(name, ItemType.System_Texture)
        {
            SysTex = sysTex;
            Grp = project.Grp.GetFileByIndex(sysTex.GrpIndex);
            if (SysTex.Screen == SysTexScreen.BOTTOM_SCREEN)
            {
                Grp.ImageForm = GraphicsFile.Form.TEXTURE;
            }
            else
            {
                Grp.ImageForm = GraphicsFile.Form.TILE;
            }
            Width = width < 0 ? Grp.Width : width;
            Height = height < 0 ? Grp.Height : height;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }

        public SKBitmap GetTexture()
        {
            if (SysTex.Screen == SysTexScreen.BOTTOM_SCREEN)
            {
                return Grp.GetImage(transparentIndex: 0);
            }
            else
            {
                SKBitmap tileBitmap = new(Width, Height);
                SKBitmap tiles = Grp.GetImage(width: SysTex.TileWidth, transparentIndex: 0);
                SKCanvas tileCanvas = new(tileBitmap);
                int currentTile = 0;
                for (int y = 0; y < tileBitmap.Height; y += SysTex.TileHeight)
                {
                    for (int x = 0; x < tileBitmap.Width; x += SysTex.TileWidth)
                    {
                        SKRect crop = new(0, currentTile * SysTex.TileHeight, SysTex.TileWidth, (currentTile + 1) * SysTex.TileHeight);
                        SKRect dest = new(x, y, x + SysTex.TileWidth, y + SysTex.TileHeight);
                        tileCanvas.DrawBitmap(tiles, crop, dest);
                        currentTile++;
                    }
                }
                tileCanvas.Flush();

                return tileBitmap;
            }
        }

        public void SetTexture(SKBitmap bitmap, bool replacePalette)
        {
            if (replacePalette == false)
            {
                Grp.Palette[0] = SKColors.Transparent;
            }
            if (SysTex.Screen == SysTexScreen.BOTTOM_SCREEN)
            {
                Grp.SetImage(bitmap, replacePalette, transparentIndex: replacePalette ? 0 : -1);
            }
            else
            {
                SKBitmap tileBitmap = new(SysTex.TileWidth, bitmap.Width / SysTex.TileWidth * bitmap.Height);
                SKCanvas tileCanvas = new(tileBitmap);

                int currentTile = 0;
                for (int y = 0; y < bitmap.Height; y += SysTex.TileHeight)
                {
                    for (int x = 0; x < bitmap.Width; x += SysTex.TileWidth)
                    {
                        SKRect crop = new(x, y, x + SysTex.TileWidth, y + SysTex.TileHeight);
                        SKRect dest = new(0, currentTile * SysTex.TileHeight, SysTex.TileWidth, (currentTile + 1) * SysTex.TileHeight);
                        tileCanvas.DrawBitmap(bitmap, crop, dest);
                        currentTile++;
                    }
                }
                tileCanvas.Flush();

                Grp.SetImage(tileBitmap, replacePalette, transparentIndex: replacePalette ? 0 : -1);
            }
        }

        public void Write(Project project, ILogger log)
        {
            using MemoryStream grpStream = new();
            Grp.GetImage().Encode(grpStream, SKEncodedImageFormat.Png, 1);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{Grp.Index:X3}.png"), grpStream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{Grp.Index:X3}.gi"), Grp.GetGraphicInfoFile(), project, log);
        }
    }
}
