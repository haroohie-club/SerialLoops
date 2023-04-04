using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class BgmScriptParameter : ScriptParameter
    {
        public BackgroundMusicItem Bgm { get; set; }

        public BgmScriptParameter(string name, BackgroundMusicItem bgm) : base(name, ParameterType.BGM)
        {
            Bgm = bgm;
        }

        public override BgmScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Bgm);
        }
    }
}
