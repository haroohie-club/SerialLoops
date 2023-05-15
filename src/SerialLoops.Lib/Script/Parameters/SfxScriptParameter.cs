using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class SfxScriptParameter : ScriptParameter
    {
        public short SfxIndex { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { SfxIndex };

        public SfxScriptParameter(string name, short sfxIndex) : base(name, ParameterType.SFX)
        {
            SfxIndex = sfxIndex;
        }

        public override SfxScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, SfxIndex);
        }
    }
}
