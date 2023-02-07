namespace SerialLoops.Lib.Script.Parameters
{
    public class PaletteEffectScriptParameter : ScriptParameter
    {
        public PaletteEffect Effect { get; set; }

        public PaletteEffectScriptParameter(string name, short effect) : base(name, ParameterType.PALETTE_EFFECT)
        {
            Effect = (PaletteEffect)effect;
        }

        public enum PaletteEffect : short
        {
            DEFAULT = 216,
            INVERTED = 217,
            GRAYSCALE = 218,
            SEPIA = 219,
            DIMMED = 220,
        }
    }
}
