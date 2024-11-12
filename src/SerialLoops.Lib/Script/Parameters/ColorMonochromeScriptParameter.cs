using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ColorMonochromeScriptParameter(string name, short colorType)
    : ScriptParameter(name, ParameterType.COLOR_MONOCHROME)
{
    public ColorMonochrome ColorType { get; set; } = (ColorMonochrome)colorType;
    public override short[] GetValues(object? obj = null) => [(short)ColorType];

    public override ColorMonochromeScriptParameter Clone(Project project, EventFile eventFile)
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
