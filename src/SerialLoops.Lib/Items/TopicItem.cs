using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib.Items
{
    public class TopicItem : Item
    {
        public Topic TopicEntry { get; set; }
        public Topic HiddenMainTopic { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public TopicItem(Topic topic, Project project) : base($"{topic.Id}", ItemType.Topic)
        {
            DisplayName = $"{topic.Id} - {topic.Title.GetSubstitutedString(project)}";
            CanRename = false;
            TopicEntry = topic;
        }

        public override void Refresh(Project project, ILogger log)
        {
        }
    }
}
