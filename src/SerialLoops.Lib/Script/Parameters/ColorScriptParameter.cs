﻿using HaruhiChokuretsuLib.Archive.Event;
using SkiaSharp;

namespace SerialLoops.Lib.Script.Parameters;

public class ColorScriptParameter : ScriptParameter
{
    private byte _red;
    private byte _green;
    private byte _blue;

    public SKColor Color { get; set; }
    public override short[] GetValues(object obj = null) => [_red, _green, _blue];

    public override string GetValueString(Project project)
    {
        return project.Localize(Color.ToString());
    }

    public ColorScriptParameter(string name, short red) : base(name, ParameterType.COLOR)
    {
        _red = (byte)red;
    }

    public override ColorScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, _red);
    }

    public void SetGreen(short green)
    {
        _green = (byte)green;
    }

    public void SetBlue(short blue)
    {
        _blue = (byte)blue;

        Color = new(_red, _green, _blue);
    }
}
