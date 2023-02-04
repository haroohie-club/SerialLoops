namespace SerialLoops.Lib.Script
{
    public class TopicScriptParameter : ScriptParameter
    {
        public short TopicId { get; set; }

        public TopicScriptParameter(string name, short topic) : base(name, ParameterType.TOPIC)
        {
            TopicId = topic;
        }
    }
}
