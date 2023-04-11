using SkiaSharp;
using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class PaletteEffectScriptParameter : ScriptParameter
    {
        public PaletteEffect Effect { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)Effect };

        public PaletteEffectScriptParameter(string name, short effect) : base(name, ParameterType.PALETTE_EFFECT)
        {
            Effect = (PaletteEffect)effect;
        }

        public override PaletteEffectScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, (short)Effect);
        }

        public enum PaletteEffect : short
        {
            DEFAULT = 216,
            INVERTED = 217,
            GRAYSCALE = 218,
            SEPIA = 219,
            DIMMED = 220,
        }

        public static SKPaint IdentityPaint { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                1.00f, 0.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 1.00f, 0.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 1.00f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint InvertedPaint { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                -1.0f, 0.00f, 0.00f, 0.00f, 1.0f,
                0.00f, -1.0f, 0.00f, 0.00f, 1.0f,
                0.00f, 0.00f, -1.0f, 0.00f, 1.0f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint GrayscalePaint { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.21f, 0.72f, 0.07f, 0.00f, 0.00f,
                0.21f, 0.72f, 0.07f, 0.00f, 0.00f,
                0.21f, 0.72f, 0.07f, 0.00f, 0.00f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint SepiaPaint { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.393f, 0.769f, 0.189f, 0.00f, 0.00f,
                0.349f, 0.686f, 0.168f, 0.00f, 0.00f,
                0.272f, 0.534f, 0.131f, 0.00f, 0.00f,
                0.000f, 0.000f, 0.000f, 1.00f, 0.00f,
            }),
        };
        public static SKPaint DimmedPaint { get; } = new()
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.40f, 0.00f, 0.00f, 0.00f, 0.0f,
                0.00f, 0.40f, 0.00f, 0.00f, 0.0f,
                0.00f, 0.00f, 0.40f, 0.00f, 0.0f,
                0.00f, 0.00f, 0.00f, 1.00f, 0.00f,
            }),
        };
    }
}
