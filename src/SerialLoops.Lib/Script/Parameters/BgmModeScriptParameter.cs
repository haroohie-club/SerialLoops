using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class BgmModeScriptParameter : ScriptParameter
    {
        public BgmMode Mode { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)Mode };

        public BgmModeScriptParameter(string name, short mode) : base(name, ParameterType.BGM_MODE)
        {
            Mode = (BgmMode)mode;
        }

        public enum BgmMode : short
        {
            START = 2,
            STOP = 4,
        }

        public override BgmModeScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, (short)Mode);
        }
    }
}
