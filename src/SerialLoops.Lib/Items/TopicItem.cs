using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class TopicItem : Item
    {
        public TopicStruct Topic { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public TopicItem(TopicStruct topicStruct, Project project) : base($"{topicStruct.Id}", ItemType.Topic)
        {
            DisplayName = $"{topicStruct.Title.GetSubstitutedString(project)}";
            Topic = topicStruct;
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
                .Where(t => t.c.Parameters[0] == Topic.Id).ToArray();
        }
    }
}
