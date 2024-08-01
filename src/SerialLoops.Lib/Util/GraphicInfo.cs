using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;

namespace SerialLoops.Lib.Util
{
    public class GraphicInfo
    {
        public string Determinant { get; set; }
        public GraphicsFile.TileForm TileForm { get; set; }
        public short Unknown08 { get; set; }
        public short RenderWidth { get; set; }
        public short RenderHeight { get; set; }
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
            RenderWidth = file.RenderWidth;
            RenderHeight = file.RenderHeight;
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
            file.RenderWidth = RenderWidth;
            file.RenderHeight = RenderHeight;
            file.Unknown12 = Unknown12;
        }
    }
}
