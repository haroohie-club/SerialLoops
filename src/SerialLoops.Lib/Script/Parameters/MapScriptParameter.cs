using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class MapScriptParameter : ScriptParameter
    {
        public MapItem Map { get; set; }

        public MapScriptParameter(string name, MapItem map) : base(name, ParameterType.MAP)
        {
            Map = map;
        }
    }
}
