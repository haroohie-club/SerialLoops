namespace SerialLoops.Lib.Script
{
    public class ScreenScriptParameter : ScriptParameter
    {
        public DsScreen Screen { get; set; }

        public ScreenScriptParameter(string name, short screen) : base(name, ParameterType.SCREEN)
        {
            Screen = (DsScreen)screen;
        }

        public enum DsScreen : short
        {
            BOTTOM = 0,
            TOP = 1,
            BOTH = 2,
        }
    }
}
