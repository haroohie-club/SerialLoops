using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class VoicedLineScriptParameter(string name, VoicedLineItem vce)
    : ScriptParameter(name, ParameterType.VOICE_LINE)
{
    public VoicedLineItem VoiceLine { get; set; } = vce;
    public override short[] GetValues(object? obj = null) => [(short)(VoiceLine?.Index ?? 0)];

    public override VoicedLineScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, VoiceLine);
    }
}
