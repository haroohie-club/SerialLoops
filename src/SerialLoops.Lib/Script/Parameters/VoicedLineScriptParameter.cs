using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class VoicedLineScriptParameter : ScriptParameter
    {
        public VoicedLineItem VoiceLine { get; set; }

        public VoicedLineScriptParameter(string name, VoicedLineItem vce) : base(name, ParameterType.VOICE_LINE)
        {
            VoiceLine = vce;
        }
    }
}
