using System.Linq;
using HaroohieClub.NitroPacker;
using HaroohieClub.NitroPacker.Nitro.Card.Banners;
using HaroohieClub.NitroPacker.Nitro.Gx;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;

namespace SerialLoops.Lib;

public class ProjectSettings
{
    private ILogger _log;
    public NdsProjectFile File { get; }
    private BannerV1 _banner => (BannerV1)File?.RomInfo.Banner.Banner ?? new();

    public ProjectSettings()
    {
    }

    public ProjectSettings(NdsProjectFile file, ILogger log)
    {
        File = file;
        _log = log;
    }

    public string Name
    {
        get => _banner.GameName[0];
        set
        {
            string name = value.Length > 128 ? value[..128] : value;
            for (int i = 0; i < _banner.GameName.Length; i++)
            {
                _banner.GameName[i] = name.Replace("\r\n", "\n");
            }
        }
    }

    public SKBitmap Icon
    {
        get
        {
            Rgba8Bitmap bitmap;
            try
            {
                bitmap = _banner?.GetIcon() ?? new(16, 16);
            }
            catch
            {
                bitmap = new(16, 16);
            }
            return new(bitmap?.Width ?? 16, bitmap?.Height ?? 16)
            {
                Pixels = bitmap?.Pixels.Select(p => new SKColor(p)).ToArray(),
            };
        }
        set
        {
            GraphicsFile grp = new()
            {
                Name = "ICON",
                PixelData = [],
                PaletteData = [],
            };
            grp.Initialize([], 0, _log);
            grp.FileFunction = GraphicsFile.Function.SHTX;
            grp.ImageForm = GraphicsFile.Form.TILE;
            grp.ImageTileForm = GraphicsFile.TileForm.GBA_4BPP;
            grp.Width = 32;
            grp.Height = 32;
            grp.Palette = new(new SKColor[16]);
            grp.SetImage(value, setPalette: true, transparentIndex: 0, newSize: true);

            _banner.Image = grp.PixelData.ToArray();
            _banner.Palette = grp.PaletteData.ToArray();
        }
    }
}
