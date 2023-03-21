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

        public override BgmScriptParameter Clone()
        {
            return new(Name, Bgm);
        }
    }
}
