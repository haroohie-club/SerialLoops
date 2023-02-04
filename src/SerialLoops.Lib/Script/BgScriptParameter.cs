using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script
{
    public class BgScriptParameter : ScriptParameter
    {
        public BackgroundItem Background { get; set; }

        public BgScriptParameter(string name, BackgroundItem background) : base(name, ParameterType.BG)
        {
            Background = background;
        }
    }
}
