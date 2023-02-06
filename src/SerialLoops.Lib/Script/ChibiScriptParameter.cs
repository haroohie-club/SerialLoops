using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script
{
    public class ChibiScriptParameter : ScriptParameter
    {
        public ChibiItem Chibi { get; set; }

        public ChibiScriptParameter(string name, ChibiItem chibi) : base(name, ParameterType.CHIBI)
        {
            Chibi = chibi;
        }
    }
}
