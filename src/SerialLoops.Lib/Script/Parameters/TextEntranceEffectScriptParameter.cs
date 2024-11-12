using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class TextEntranceEffectScriptParameter(string name, short entranceEffect)
    : ScriptParameter(name, ParameterType.TEXT_ENTRANCE_EFFECT)
{
    public TextEntranceEffect EntranceEffect { get; set; } = (TextEntranceEffect)entranceEffect;
    public override short[] GetValues(object? obj = null) => [(short)EntranceEffect];

    public override TextEntranceEffectScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)EntranceEffect);
    }

    public enum TextEntranceEffect : short
    {
        NORMAL = 0,
        SHRINK_IN = 1,
        TERMINAL_TYPING = 2,
    }
}
