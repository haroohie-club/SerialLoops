using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class TopicScriptParameter(string name, short topic) : ScriptParameter(name, ParameterType.TOPIC)
{
    public short TopicId { get; set; } = topic;
    public override short[] GetValues(object? obj = null) => [TopicId];

    public override TopicScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, TopicId);
    }
}
