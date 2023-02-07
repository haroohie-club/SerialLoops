namespace SerialLoops.Lib.Script.Parameters
{
    public class BgmModeScriptParameter : ScriptParameter
    {
        public BgmMode Mode { get; set; }

        public BgmModeScriptParameter(string name, short mode) : base(name, ParameterType.BGM_MODE)
        {
            Mode = (BgmMode)mode;
        }

        public enum BgmMode : short
        {
            START = 2,
            STOP = 4,
        }
    }
}
