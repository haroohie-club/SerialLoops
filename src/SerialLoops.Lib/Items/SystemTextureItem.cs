using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items;

public class SystemTextureItem : Item
{
    public SystemTexture SysTex { get; set; }
    public GraphicsFile Grp { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    private const string MainPalette = "#ff00f800,#fff00080,#ff80f000,#ff00f080,#ff00f800,#ff00f800,#ff00f800,#ff482828,#ff684038,#ff905850,#ffb88070,#ffc8a080,#ffd8b090,#ffe8c8a0,#fff8d8b0,#fff8e8c0,#fff00000,#fff02800,#fff04000,#fff05000,#fff06000,#fff07000,#fff08000,#fff09000,#fff0a000,#fff0b000,#fff0c000,#fff0d000,#fff0e000,#fff0f000,#ffe0f000,#ffd0f000,#ffc0f000,#ffb0f000,#ffa0f000,#ff90f000,#ff80f000,#ff68f000,#ff50f000,#ff30f000,#ff00f000,#ff00f040,#ff00f068,#ff00f090,#ff00f0b0,#ff00f0d0,#ff00f0f0,#ff00e0f0,#ff00d0f0,#ff00c0f0,#ff00b0f0,#ff00a0f0,#ff0090f0,#ff0080f0,#ff0070f0,#ff0060f0,#ff0050f0,#ff0040f0,#ff0030f0,#ff0020f0,#ff0000f0,#ff2000f0,#ff3000f0,#ff4000f0,#ff5000f0,#ff6000f0,#ff7000f0,#ff8000f0,#ff9000f0,#ffa000f0,#ffb000f0,#ffc000f0,#ffd000f0,#ffe000f0,#fff000f0,#fff000d0,#fff000b0,#fff00090,#fff00068,#fff00040,#fff8f8f8,#fff0f0f0,#ffe8e8e8,#ffe0e0e0,#ffd8d8d8,#ffd0d0d0,#ffc8c8c8,#ffc0c0c0,#ffb8b8b8,#ffb0b0b0,#ffa8a8a8,#ffa0a0a0,#ff989898,#ff909090,#ff888888,#ff808080,#ff787878,#ff707070,#ff686868,#ff606060,#ff585858,#ff505050,#ff484848,#ff404040,#ff383838,#ff303030,#ff282828,#ff202020,#ff181818,#ff101010,#ff080808,#ff000000,#ff281010,#ff502020,#ff783838,#ffa05050,#ffc86868,#fff08080,#fff8a0a0,#fff8c0c0,#fff8e0e0,#ff282810,#ff505020,#ff787838,#ffa0a050,#ffc8c868,#fff0f080,#fff8f8a0,#fff8f8c0,#fff8f8e0,#ff102810,#ff285028,#ff407840,#ff50a050,#ff68c868,#ff80f080,#ffa0f8a0,#ffc0f8c0,#ffe0f8e0,#ff102828,#ff205050,#ff387878,#ff50a0a0,#ff68c8c8,#ff80f0f0,#ffa0f8f8,#ffc0f8f8,#ffe0f8f8,#ff101028,#ff202050,#ff383878,#ff5050a0,#ff6868c8,#ff8080f0,#ffa0a0f8,#ffc0c0f8,#ffe0e0f8,#ff281028,#ff502050,#ff783878,#ffa050a0,#ffc868c8,#fff080f0,#fff8a0f8,#fff8c0f8,#fff8e0f8,#ff280010,#ff500020,#ff780038,#ffa00050,#ffc80068,#fff03898,#fff868b0,#fff898c8,#fff8c8e0,#ff281000,#ff502000,#ff783800,#ffa05000,#ffc86800,#fff09838,#fff8b068,#fff8c898,#fff8e0c8,#ff102800,#ff205000,#ff387800,#ff50a000,#ff68c800,#ff98f030,#ffb0f860,#ffc8f898,#ffe0f8c8,#ff002810,#ff005020,#ff007838,#ff00a050,#ff00c868,#ff30f098,#ff60f8b0,#ff98f8c8,#ffc8f8e0,#ff001028,#ff002050,#ff003878,#ff0050a0,#ff0068c8,#ff3098f0,#ff60b0f8,#ff98c8f8,#ffc8e0f8,#ff100028,#ff200050,#ff380078,#ff5000a0,#ff6800c8,#ff9830f0,#ffb060f8,#ffc898f8,#ffe0c8f8,#ff300000,#ff600000,#ff900000,#ffc00000,#fff03030,#fff86060,#ff303000,#ff606000,#ff909000,#ffc0c000,#fff0f030,#fff8f860,#ff003000,#ff006000,#ff009000,#ff00c000,#ff30f030,#ff60f860,#ff003030,#ff006060,#ff009090,#ff00c0c0,#ff30f0f0,#ff60f8f8,#ff000030,#ff000060,#ff000090,#ff0000c0,#ff3030f0,#ff6060f8,#ff300030,#ff600060,#ff900090,#ffc000c0,#fff030f0,#fff860f8";
    private const string ExtraPalette = "#ff00f800,#ff080008,#ff000020,#ff000040,#ff080810,#ff000830,#ff100810,#ff100820,#ff180810,#ff180820,#ff181010,#ff081818,#ff001058,#ff101828,#ff181038,#ff181818,#ff201028,#ff281018,#ff081840,#ff201030,#ff201810,#ff281030,#ff301028,#ff102038,#ff202020,#ff381810,#ff301840,#ff282028,#ff381830,#ff381838,#ff302020,#ff083818,#ff381848,#ff182060,#ff382020,#ff202838,#ff002088,#ff302820,#ff302830,#ff382820,#ff382050,#ff203040,#ff502028,#ff202870,#ff103860,#ff303040,#ff383038,#ff283848,#ff483028,#ff383848,#ff403838,#ff602838,#ff203880,#ff782028,#ff105820,#ff284058,#ff503830,#ff583830,#ff204870,#ff404048,#ff484038,#ff304858,#ff484050,#ff584038,#ff783038,#ff285068,#ff404850,#ff504848,#ff684030,#ffc81808,#ff504858,#ff604840,#ff585808,#ff405850,#ff385870,#ff485068,#ff704830,#ff485850,#ff585050,#ff983050,#ff385880,#ff585060,#ff685040,#ff486050,#ff804838,#ff386080,#ff406850,#ff605860,#ff586820,#ff685850,#ff805038,#ff984828,#ff705848,#ff884080,#ff506070,#ff885038,#ff386890,#ffd83018,#ff406888,#ff586078,#ff686060,#ff786050,#ff706068,#ff607818,#ff905840,#ffb83880,#ff487860,#ff606870,#ff806050,#ff686870,#ff407898,#ff786860,#ff906838,#ffa86028,#ff587888,#ff986060,#ffa06048,#ff707080,#fff04028,#ffc84098,#ff787078,#ffa86048,#ff807068,#ff4880a8,#ff588860,#ff907058,#ff787878,#ff608090,#ff887870,#ffc05880,#ff5088b0,#ff589078,#ffe85820,#ff6078c8,#ffb86850,#ffb87028,#ff907870,#ff788088,#ff987860,#ffd050a0,#ff3098e0,#ff808090,#ff6888a8,#ff888080,#ffa87860,#ffb86888,#ff988070,#ff5898b8,#ff888888,#ffa08078,#ffd86098,#ffc08030,#ff809098,#ff988888,#ffa08878,#ffb88068,#ff889098,#ff7898a8,#ffa08890,#ff60b070,#ff909098,#ffb08870,#ffa09088,#fff87830,#ff60a0e0,#ff88a870,#ffe87098,#ffb89078,#ff80a0b8,#ffa09898,#ffc09070,#ffa89888,#ffd89038,#ffd08880,#ff80a8b8,#ffb09890,#ffb89888,#ff98a0b0,#ffa0a0a0,#ffc09878,#ffc090a8,#fff87898,#ffb8a080,#ffd09870,#ffb0a098,#ffb8a088,#ff70d060,#ff18e0f8,#ffc0a088,#ffa0a8b8,#ffa8a8a8,#ffc8a080,#ffd0a080,#ffb8a8a0,#ffe0a068,#ff98b8c8,#ffb0b0b0,#ffc8a898,#ffb0b0c0,#ff80c0f0,#ffd8a888,#ffb8b0b8,#ffc0b0a8,#fff0b028,#ff18f8f8,#ffc8b0a8,#ffe8a888,#ffa8b8e0,#ffd8b098,#ffc0b8c0,#ffc8b8b0,#ffa8c8b8,#ffd8b898,#ffd8b8a0,#ffa0c8e8,#ffb8c0d0,#ffc0c0c0,#ffe8b888,#ffc8c0c0,#ffb0d0b8,#ffc8c0c8,#ffd0c0b8,#ffd8c0a8,#ffb8c8e0,#ffe8c0a0,#ffc8c8d0,#fff0c090,#ffd0c8c0,#ffc8c8d8,#ffe8c8b0,#ffd0d0d0,#ffd8d0c0,#fff8e018,#ffc0d8e0,#ffe0d8a8,#fff0d0a8,#fff0d0b0,#ffd8d8d8,#fff8d0b0,#fff8d888,#ffe0d8d0,#fff8d0c0,#fff0d8c0,#ffa0f8f8,#ffc0e8f8,#fff8f820,#ffe0e0e0,#ffe8e0e0,#ffd8e8f8,#fff8e8a8,#ffe8e8e8,#fff8e8c0,#ffe0f0f8,#fff0f0f0,#fff8f8b8,#fff8f8f8";
    private const string ChessPalette = "#ff00f800,#ff000000,#ff080808,#ff100818,#ff101008,#ff180820,#ff101010,#ff200810,#ff280028,#ff181010,#ff181020,#ff281010,#ff181818,#ff201028,#ff000090,#ff201810,#ff201818,#ff400040,#ff281818,#ff381018,#ff480818,#ff182020,#ff201838,#ff202020,#ff201858,#ff282020,#ff301840,#ff401818,#ff302018,#ff382018,#ff481818,#ff401838,#ff282828,#ff183030,#ff382030,#ff501820,#ff302050,#ff680068,#ff382820,#ff183830,#ff302068,#ff601820,#ff303030,#ff382848,#ff403008,#ff502818,#ff203848,#ff383038,#ff403020,#ff403028,#ff204030,#ff402860,#ff483028,#ff781828,#ffb80000,#ff383838,#ff583010,#ff602050,#ff702028,#ff403830,#ff204838,#ff403840,#ff503048,#ff583810,#ff384038,#ff403858,#ff503830,#ff5000f0,#ff683020,#ff980098,#ff802030,#ff205040,#ff483078,#ff404040,#ff583838,#ff484038,#ff504030,#ff604018,#ff404848,#ff504048,#ff882830,#ff584038,#ff285840,#ff484060,#ff484848,#ff783828,#ff504840,#ff604818,#ff504848,#ff583888,#ff684038,#ff504850,#ff903030,#ff286048,#ff604838,#ff385868,#ff505050,#ff585040,#ffa83018,#ff584870,#ff604858,#ffa03030,#ff4030d8,#ff804030,#fff80040,#ff485850,#ff785000,#ff605040,#ff605048,#ff685040,#ff605058,#ff486050,#fff80070,#ff585858,#ff605848,#ffa83838,#ff605850,#ff885018,#ff605080,#ffa03080,#ff685858,#ff706020,#ff785848,#ff406888,#ff606060,#ff905038,#ffb84040,#ffd83820,#ff885078,#ff706068,#fff83000,#ff806048,#ff686868,#ff985840,#ff407890,#ffa06018,#ff807020,#ff886848,#ff687078,#ff8858a0,#ff906060,#ff0090f0,#ff786888,#ff806870,#ff707078,#ff906858,#ffa86048,#ff787878,#ff907060,#ff4888a8,#ff887080,#ff908020,#ffa87810,#ffc06820,#ff6060f8,#ffb86850,#ff08c060,#ff987860,#ff808080,#ff907888,#ffb87050,#ffb88000,#ffb860a8,#ffb07858,#ff989028,#ff9870c8,#ff5898b8,#ff888888,#ffd07820,#ff988098,#ff988878,#ffb08068,#ffa88870,#ff909090,#ffd08818,#ffa08898,#ffc870b8,#ffa8a020,#ffd08060,#ffb088a0,#ff989898,#ffe09000,#ffb89070,#ff80a0b0,#ffa89878,#ffd89028,#ffa098a0,#ffd080b8,#ffe08868,#ff00e0f0,#ffb0a080,#ffc89878,#ffa8a0a8,#ffe8a000,#ffb898b8,#ffe0a030,#ff00f0f0,#ff88b8b0,#ffb8a888,#ffc0b820,#fff09070,#ffd890c0,#ff68c8c8,#ffb0a8b0,#ffd0a088,#ffe8a830,#fff0a830,#ffb0b0b8,#ffc0b090,#fff8b000,#ffc0a8c0,#ffd8a888,#ffd0c020,#ffc0a8d8,#ffb8c070,#ff98c0d0,#ffb8b8b8,#ffd8b090,#ffc8b0c8,#ffe8a0c8,#ffe0b880,#ffc8c098,#ffc0c0c0,#ffe0b8a0,#fff8a8d0,#ffd8b8d8,#ffc8c8c8,#ffe8c0a0,#ffc8c8d0,#ffe8d830,#ffd8d0a0,#ffe8c8b0,#ffd8d0c0,#ffd0d0d8,#fff8e038,#fff0d0a8,#fff0d0b0,#ffe8d8a0,#ffd8d8d8,#fff8d0b8,#fff8d0c0,#ff98f8f8,#ffe0d8e8,#fff0d8c0,#ffe8e0a8,#fff0f060,#ffe0e0e0,#ffe0e0e8,#ffe8e8d8,#ffc0f8f8,#ffe8e8e0,#ffe8e8e8,#fff8e8d0,#fff0f0f0,#ffe0f8f8,#fff8f8f8";
    private static readonly SKColor s_transparent = new(0, 248, 0);

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
            using SKCanvas tileCanvas = new(tileBitmap);
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
        else if (Grp.Palette[0] == s_transparent)
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
            using SKCanvas tileCanvas = new(tileBitmap);

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

            Grp.SetImage(tileBitmap, newSize: true);
        }
        Grp.Palette[0] = s_transparent;
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
        // We don't use the functions below so we only stringify the palette once for perf
        string paletteString = string.Join(',', Grp.Palette.Select(c => c.ToString()));
        return MainPalette.Equals(paletteString, StringComparison.OrdinalIgnoreCase)
               || ExtraPalette.Equals(paletteString, StringComparison.OrdinalIgnoreCase)
               || ChessPalette.Equals(paletteString, StringComparison.OrdinalIgnoreCase);
    }

    public bool UsesMainPalette()
    {
        return MainPalette.Equals(string.Join(',', Grp.Palette.Select(c => c.ToString())), StringComparison.OrdinalIgnoreCase);
    }
    public bool UsesExtraPalette()
    {
        return ExtraPalette.Equals(string.Join(',', Grp.Palette.Select(c => c.ToString())), StringComparison.OrdinalIgnoreCase);
    }
    public bool UsesChessPalette()
    {
        return ChessPalette.Equals(string.Join(',', Grp.Palette.Select(c => c.ToString())), StringComparison.OrdinalIgnoreCase);
    }
}
