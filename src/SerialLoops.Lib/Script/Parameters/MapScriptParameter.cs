﻿using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class MapScriptParameter : ScriptParameter
{
    public MapItem Map { get; set; }
    public override short[] GetValues(object obj = null) => [(short)Map.Map.Index];

    public override string GetValueString(Project project)
    {
        return Map?.DisplayName;
    }

    public MapScriptParameter(string name, MapItem map) : base(name, ParameterType.MAP)
    {
        Map = map;
    }

    public override MapScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Map);
    }
}
