namespace SerialLoops.Lib.Script.Parameters
{
    public class VoicedLineScriptParameter : ScriptParameter
    {
        public short VoiceIndex { get; set; }

        public VoicedLineScriptParameter(string name, short voiceIndex) : base(name, ParameterType.VOICE_LINE)
        {
            VoiceIndex = voiceIndex;
        }
    }
}
