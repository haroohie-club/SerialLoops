namespace SerialLoops.Lib.Script.Parameters
{
    public class ColorMonochromeScriptParameter : ScriptParameter
    {
        public ColorMonochrome ColorType { get; set; }

        public ColorMonochromeScriptParameter(string name, short colorType) : base(name, ParameterType.COLOR_MONOCHROME)
        {
            ColorType = (ColorMonochrome)colorType;
        }

        public override ColorMonochromeScriptParameter Clone()
        {
            return new(Name, (short)ColorType);
        }

        public enum ColorMonochrome : short
        {
            CUSTOM = 0,
            BLACK = 1,
            WHITE = 2,
        }
    }
}
