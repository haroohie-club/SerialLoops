namespace SerialLoops.Lib.Script.Parameters
{
    public class TextEntranceEffectScriptParameter : ScriptParameter
    {
        public TextEntranceEffect EntranceEffect { get; set; }

        public TextEntranceEffectScriptParameter(string name, short entranceEffect) : base(name, ParameterType.TEXT_ENTRANCE_EFFECT)
        {
            EntranceEffect = (TextEntranceEffect)entranceEffect;
        }

        public enum TextEntranceEffect : short
        {
            NORMAL = 0,
            SHRINK_IN = 1,
            TERMINAL_TYPING = 2,
        }
    }
}
