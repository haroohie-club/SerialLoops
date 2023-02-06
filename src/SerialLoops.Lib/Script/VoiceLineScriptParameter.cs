namespace SerialLoops.Lib.Script
{
    public class VoiceLineScriptParameter : ScriptParameter
    {
        public short VoiceIndex { get; set; }

        public VoiceLineScriptParameter(string name, short voiceIndex) : base(name, ParameterType.VOICE_LINE)
        {
            VoiceIndex = voiceIndex;
        }
    }
}
