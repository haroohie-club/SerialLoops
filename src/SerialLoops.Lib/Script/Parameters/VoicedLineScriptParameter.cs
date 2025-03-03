﻿using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class VoicedLineScriptParameter : ScriptParameter
{
    public VoicedLineItem VoiceLine { get; set; }
    public override short[] GetValues(object obj = null) => [(short)(VoiceLine?.Index ?? 0)];

    public override string GetValueString(Project project)
    {
        return VoiceLine?.DisplayName;
    }

    public VoicedLineScriptParameter(string name, VoicedLineItem vce) : base(name, ParameterType.VOICE_LINE)
    {
        VoiceLine = vce;
    }

    public override VoicedLineScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, VoiceLine);
    }
}
