using System;
using System.Linq;
using HaroohieClub.NitroPacker.Core;
using HaroohieClub.NitroPacker.Nitro.Gx;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using static HaroohieClub.NitroPacker.Nitro.Card.Rom.RomBanner;

namespace SerialLoops.Lib
{
    public class ProjectSettings(NdsProjectFile file, ILogger log)
    {
        public NdsProjectFile File { get; } = file;
        private BannerV1 Banner => File.RomInfo.Banner.Banner;
        private readonly ILogger _log = log;

        public string Name {
            get => Banner.GameName[0];
            set
            {
                string name = value.Length > 128 ? value[..63] : value;
                for (int i = 0; i < Banner.GameName.Length; i++)
                {
                    Banner.GameName[i] = name.Replace("\r\n", "\n");
                }
            }
        }

        public SKBitmap Icon
        {
            get
            {
                Rgba8Bitmap bitmap = Banner.GetIcon();
                return new(bitmap.Width, bitmap.Height)
                {
                    Pixels = bitmap.Pixels.Select(p => new SKColor(p)).ToArray()
                };
            }
            set
            {
                GraphicsFile grp = new()
                {
                    Name = "ICON",
                    PixelData = new(),
                    PaletteData = new(),
                };
                grp.Initialize(Array.Empty<byte>(), 0, _log);
                grp.FileFunction = GraphicsFile.Function.SHTX;
                grp.ImageForm = GraphicsFile.Form.TILE;
                grp.ImageTileForm = GraphicsFile.TileForm.GBA_4BPP;
                grp.Width = 32;
                grp.Height = 32;
                grp.Palette = new(new SKColor[16]);
                grp.SetImage(value, setPalette: true, transparentIndex: 0, newSize: true);

                Banner.Image = grp.PixelData.ToArray();
                Banner.Pltt = grp.PaletteData.ToArray();
            }
        }
    }
}
