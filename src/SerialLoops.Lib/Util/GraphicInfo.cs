using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SerialLoops.Lib.Util
{
    public class GraphicInfo
    {
        public string Determinant { get; set; }
        public GraphicsFile.TileForm TileForm { get; set; }
        public short Unknown08 { get; set; }
        public short TileWidth { get; set; }
        public short TileHeight { get; set; }
        public short Unknown10 { get; set; }
        public short Unknown12 { get; set; }
        public string PaletteString { get; set; }

        [JsonIgnore]
        public List<SKColor> Palette => PaletteString.Split(',').Select(SKColor.Parse).ToList();

        public GraphicInfo()
        {
        }

        public GraphicInfo(GraphicsFile file)
        {
            Determinant = file.Determinant;
            Unknown08 = file.Unknown08;
            TileForm = file.ImageTileForm;
            TileWidth = file.TileWidth;
            TileHeight = file.TileHeight;
            Unknown10 = file.Unknown10;
            Unknown12 = file.Unknown12;
            PaletteString = string.Join(',', file.Palette.Select(c => c.ToString()));
        }

        public void Set(GraphicsFile file)
        {
            SetWithoutPalette(file);
            file.SetPalette(Palette);
        }

        public void SetWithoutPalette(GraphicsFile file)
        {
            file.Determinant = Determinant;
            file.ImageTileForm = TileForm;
            file.Unknown08 = Unknown08;
            file.TileWidth = TileWidth;
            file.TileHeight = TileHeight;
            file.Unknown10 = Unknown10;
            file.Unknown12 = Unknown12;
        }
    }
}
