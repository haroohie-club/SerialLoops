using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using System.Linq;

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
            PopulateScriptUses(project);
        }

        public override void Refresh(Project project, ILogger log)
        {
            PopulateScriptUses(project);
        }

        public void PopulateScriptUses(Project project)
        {
            ScriptUses = project.Evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.TOPIC_GET.ToString()).Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[0] == TopicEntry.Id).ToArray();
        }
    }
}
