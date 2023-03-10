namespace SerialLoops.Lib.Script.Parameters
{
    public class TopicScriptParameter : ScriptParameter
    {
        public short TopicId { get; set; }

        public TopicScriptParameter(string name, short topic) : base(name, ParameterType.TOPIC)
        {
            TopicId = topic;
        }

        public override TopicScriptParameter Clone()
        {
            return new(Name, TopicId);
        }
    }
}
