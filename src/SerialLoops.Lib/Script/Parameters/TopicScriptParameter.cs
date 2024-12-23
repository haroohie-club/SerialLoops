using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class TopicScriptParameter : ScriptParameter
{
    public short TopicId { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { TopicId };

    public override string GetValueString(Project project)
    {
        return project.Localize(project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic &&
                                                           ((TopicItem)i).TopicEntry.Id == TopicId)?.DisplayName ?? TopicId.ToString());
    }

    public TopicScriptParameter(string name, short topic) : base(name, ParameterType.TOPIC)
    {
        TopicId = topic;
    }

    public override TopicScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, TopicId);
    }
}
