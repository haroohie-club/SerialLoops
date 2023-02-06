namespace SerialLoops.Lib.Script
{
    public class ColorMonochromeScriptParameter : ScriptParameter
    {
        public ColorMonochrome ColorType { get; set; }
        
        public ColorMonochromeScriptParameter(string name, short colorType) : base(name, ParameterType.COLOR_MONOCHROME)
        {
            ColorType = (ColorMonochrome)colorType;
        }

        public enum ColorMonochrome : short
        {
            CUSTOM = 0,
            BLACK = 1,
            WHITE = 2,
        }
    }
}
