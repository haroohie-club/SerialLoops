﻿using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class TextEntranceEffectScriptParameter : ScriptParameter
{
    public TextEntranceEffect EntranceEffect { get; set; }
    public override short[] GetValues(object obj = null) => [(short)EntranceEffect];

    public override string GetValueString(Project project)
    {
        return project.Localize(EntranceEffect.ToString());
    }

    public TextEntranceEffectScriptParameter(string name, short entranceEffect) : base(name, ParameterType.TEXT_ENTRANCE_EFFECT)
    {
        EntranceEffect = (TextEntranceEffect)entranceEffect;
    }

    public override TextEntranceEffectScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)EntranceEffect);
    }

    public enum TextEntranceEffect : short
    {
        NORMAL_TEXT_ENTRANCE = 0,
        SHRINK_IN = 1,
        TERMINAL_TYPING = 2,
    }
}
