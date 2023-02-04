namespace SerialLoops.Lib.Script
{
    public class ChibiEnterExitScriptParameter : ScriptParameter
    {
        public ChibiEnterExitType Mode { get; set; }

        public ChibiEnterExitScriptParameter(string name, short mode) : base(name, ParameterType.CHIBI_ENTER_EXIT)
        {
            Mode = (ChibiEnterExitType)mode;
        }

        public enum ChibiEnterExitType
        {
            ENTER = 0,
            EXIT = 1,
        }
    }
}
