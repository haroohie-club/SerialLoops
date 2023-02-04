using SkiaSharp;

namespace SerialLoops.Lib.Script
{
    public class ColorScriptParameter : ScriptParameter
    {
        public SKColor Color { get; set; }

        public ColorScriptParameter(string name, short red, short green, short blue) : base(name, ParameterType.COLOR)
        {
            Color = new((byte)red, (byte)green, (byte)blue);
        }
    }
}
