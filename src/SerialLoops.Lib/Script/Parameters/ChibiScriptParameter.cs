using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class ChibiScriptParameter : ScriptParameter
    {
        public ChibiItem Chibi { get; set; }

        public ChibiScriptParameter(string name, ChibiItem chibi) : base(name, ParameterType.CHIBI)
        {
            Chibi = chibi;
        }

        public override ChibiScriptParameter Clone()
        {
            return new(Name, Chibi);
        }
    }
}
