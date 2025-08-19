using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ColorMonochromeScriptParameter : ScriptParameter
{
    public ColorMonochrome ColorType { get; set; }
    public override short[] GetValues(object obj = null) => [(short)ColorType];

    public override string GetValueString(Project project)
    {
        return project.Localize(ColorType.ToString());
    }

    public ColorMonochromeScriptParameter(string name, short colorType) : base(name, ParameterType.COLOR_MONOCHROME)
    {
        ColorType = (ColorMonochrome)colorType;
    }

    public override ColorMonochromeScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)ColorType);
    }

    public enum ColorMonochrome : short
    {
        ColorMonoParamCustom = 0,
        ColorMonoParamBlack = 1,
        ColorMonoParamWhite = 2,
    }
}
