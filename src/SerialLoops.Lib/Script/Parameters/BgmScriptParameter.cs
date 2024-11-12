using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class BgmScriptParameter(string name, BackgroundMusicItem bgm) : ScriptParameter(name, ParameterType.BGM)
{
    public BackgroundMusicItem Bgm { get; set; } = bgm;
    public override short[] GetValues(object? obj = null) => [(short)Bgm.Index];

    public override BgmScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Bgm);
    }
}
