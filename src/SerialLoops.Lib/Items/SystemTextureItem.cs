using System;
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
    public class SystemTextureItem : Item
    {
        public SystemTexture SysTex { get; set; }
        public GraphicsFile Grp { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        private const string COMMON_PALETTE = "#ff00f800,#fff00080,#ff80f000,#ff00f080,#ff00f800,#ff00f800,#ff00f800,#ff482828,#ff684038,#ff905850,#ffb88070,#ffc8a080,#ffd8b090,#ffe8c8a0,#fff8d8b0,#fff8e8c0,#fff00000,#fff02800,#fff04000,#fff05000,#fff06000,#fff07000,#fff08000,#fff09000,#fff0a000,#fff0b000,#fff0c000,#fff0d000,#fff0e000,#fff0f000,#ffe0f000,#ffd0f000,#ffc0f000,#ffb0f000,#ffa0f000,#ff90f000,#ff80f000,#ff68f000,#ff50f000,#ff30f000,#ff00f000,#ff00f040,#ff00f068,#ff00f090,#ff00f0b0,#ff00f0d0,#ff00f0f0,#ff00e0f0,#ff00d0f0,#ff00c0f0,#ff00b0f0,#ff00a0f0,#ff0090f0,#ff0080f0,#ff0070f0,#ff0060f0,#ff0050f0,#ff0040f0,#ff0030f0,#ff0020f0,#ff0000f0,#ff2000f0,#ff3000f0,#ff4000f0,#ff5000f0,#ff6000f0,#ff7000f0,#ff8000f0,#ff9000f0,#ffa000f0,#ffb000f0,#ffc000f0,#ffd000f0,#ffe000f0,#fff000f0,#fff000d0,#fff000b0,#fff00090,#fff00068,#fff00040,#fff8f8f8,#fff0f0f0,#ffe8e8e8,#ffe0e0e0,#ffd8d8d8,#ffd0d0d0,#ffc8c8c8,#ffc0c0c0,#ffb8b8b8,#ffb0b0b0,#ffa8a8a8,#ffa0a0a0,#ff989898,#ff909090,#ff888888,#ff808080,#ff787878,#ff707070,#ff686868,#ff606060,#ff585858,#ff505050,#ff484848,#ff404040,#ff383838,#ff303030,#ff282828,#ff202020,#ff181818,#ff101010,#ff080808,#ff000000,#ff281010,#ff502020,#ff783838,#ffa05050,#ffc86868,#fff08080,#fff8a0a0,#fff8c0c0,#fff8e0e0,#ff282810,#ff505020,#ff787838,#ffa0a050,#ffc8c868,#fff0f080,#fff8f8a0,#fff8f8c0,#fff8f8e0,#ff102810,#ff285028,#ff407840,#ff50a050,#ff68c868,#ff80f080,#ffa0f8a0,#ffc0f8c0,#ffe0f8e0,#ff102828,#ff205050,#ff387878,#ff50a0a0,#ff68c8c8,#ff80f0f0,#ffa0f8f8,#ffc0f8f8,#ffe0f8f8,#ff101028,#ff202050,#ff383878,#ff5050a0,#ff6868c8,#ff8080f0,#ffa0a0f8,#ffc0c0f8,#ffe0e0f8,#ff281028,#ff502050,#ff783878,#ffa050a0,#ffc868c8,#fff080f0,#fff8a0f8,#fff8c0f8,#fff8e0f8,#ff280010,#ff500020,#ff780038,#ffa00050,#ffc80068,#fff03898,#fff868b0,#fff898c8,#fff8c8e0,#ff281000,#ff502000,#ff783800,#ffa05000,#ffc86800,#fff09838,#fff8b068,#fff8c898,#fff8e0c8,#ff102800,#ff205000,#ff387800,#ff50a000,#ff68c800,#ff98f030,#ffb0f860,#ffc8f898,#ffe0f8c8,#ff002810,#ff005020,#ff007838,#ff00a050,#ff00c868,#ff30f098,#ff60f8b0,#ff98f8c8,#ffc8f8e0,#ff001028,#ff002050,#ff003878,#ff0050a0,#ff0068c8,#ff3098f0,#ff60b0f8,#ff98c8f8,#ffc8e0f8,#ff100028,#ff200050,#ff380078,#ff5000a0,#ff6800c8,#ff9830f0,#ffb060f8,#ffc898f8,#ffe0c8f8,#ff300000,#ff600000,#ff900000,#ffc00000,#fff03030,#fff86060,#ff303000,#ff606000,#ff909000,#ffc0c000,#fff0f030,#fff8f860,#ff003000,#ff006000,#ff009000,#ff00c000,#ff30f030,#ff60f860,#ff003030,#ff006060,#ff009090,#ff00c0c0,#ff30f0f0,#ff60f8f8,#ff000030,#ff000060,#ff000090,#ff0000c0,#ff3030f0,#ff6060f8,#ff300030,#ff600060,#ff900090,#ffc000c0,#fff030f0,#fff860f8";
        private static SKColor TRANSPARENT = new(0, 248, 0);

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

        public void SetTexture(SKBitmap bitmap, bool replacePalette, ILogger log)
        {
            List<SKColor> replacedPalette = [];
            if (replacePalette)
            {
                replacedPalette = Helpers.GetPaletteFromImage(bitmap, 255, log);
                replacedPalette.Insert(0, SKColors.Transparent);
                Grp.SetPalette(replacedPalette);
            }
            else if (Grp.Palette[0] == TRANSPARENT)
            {
                Grp.Palette[0] = SKColors.Transparent;
            }

            if (SysTex.Screen == SysTexScreen.BOTTOM_SCREEN)
            {
                Grp.SetImage(bitmap);
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

                Grp.SetImage(tileBitmap);
            }
            Grp.Palette[0] = TRANSPARENT;
        }

        public void Write(Project project, ILogger log)
        {
            using MemoryStream grpStream = new();
            Grp.GetImage().Encode(grpStream, SKEncodedImageFormat.Png, 1);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{Grp.Index:X3}.png"), grpStream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{Grp.Index:X3}.gi"), Grp.GetGraphicInfoFile(), project, log);
        }

        public bool UsesCommonPalette()
        {
            return COMMON_PALETTE.Equals(string.Join(',', Grp.Palette.Select(c => c.ToString())), StringComparison.OrdinalIgnoreCase);
        }
    }
}
