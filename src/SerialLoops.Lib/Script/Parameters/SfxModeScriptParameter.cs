namespace SerialLoops.Lib.Script.Parameters
{
    public class SfxModeScriptParameter : ScriptParameter
    {
        public SfxMode Mode { get; set; }

        public SfxModeScriptParameter(string name, short mode) : base(name, ParameterType.SFX_MODE)
        {
            Mode = (SfxMode)mode;
        }

        public override SfxModeScriptParameter Clone()
        {
            return new(Name, (short)Mode);
        }

        public enum SfxMode : short
        {
            START = 6,
            STOP = 7,
        }
    }
}
