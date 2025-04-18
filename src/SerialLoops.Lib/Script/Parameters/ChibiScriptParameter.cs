﻿using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class ChibiScriptParameter : ScriptParameter
{
    public ChibiItem Chibi { get; set; }
    public override short[] GetValues(object obj = null) => [(short)Chibi.TopScreenIndex];

    public override string GetValueString(Project project)
    {
        return Chibi.DisplayName;
    }

    public ChibiScriptParameter(string name, ChibiItem chibi) : base(name, ParameterType.CHIBI)
    {
        Chibi = chibi;
    }

    public override ChibiScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Chibi);
    }
}
