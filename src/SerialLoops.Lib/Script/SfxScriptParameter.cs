namespace SerialLoops.Lib.Script
{
    public class SfxScriptParameter : ScriptParameter
    {
        public short SfxIndex { get; set; }

        public SfxScriptParameter(string name, short sfxIndex) : base(name, ParameterType.SFX)
        {
            SfxIndex = sfxIndex;
        }
    }
}
