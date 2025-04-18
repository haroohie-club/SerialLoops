﻿using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class BgmScriptParameter : ScriptParameter
{
    public BackgroundMusicItem Bgm { get; set; }
    public override short[] GetValues(object obj = null) => [(short)Bgm.Index];

    public override string GetValueString(Project project)
    {
        return Bgm?.DisplayName;
    }

    public BgmScriptParameter(string name, BackgroundMusicItem bgm) : base(name, ParameterType.BGM)
    {
        Bgm = bgm;
    }

    public override BgmScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Bgm);
    }
}
