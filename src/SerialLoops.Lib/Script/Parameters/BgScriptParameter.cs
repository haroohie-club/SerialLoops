using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class BgScriptParameter : ScriptParameter
    {
        public BackgroundItem Background { get; set; }
        public bool Kinetic { get; set; }

        public BgScriptParameter(string name, BackgroundItem background, bool kinetic) : base(name, ParameterType.BG)
        {
            Background = background;
            Kinetic = kinetic;
        }

        public override BgScriptParameter Clone()
        {
            return new(Name, Background, Kinetic);
        }
    }
}
