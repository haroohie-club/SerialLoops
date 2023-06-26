using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class SfxScriptParameter : ScriptParameter
    {
        public SfxItem Sfx { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { Sfx.Index };

        public SfxScriptParameter(string name, SfxItem sfx) : base(name, ParameterType.SFX)
        {
            Sfx = sfx;
        }

        public override SfxScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Sfx);
        }
    }
}
