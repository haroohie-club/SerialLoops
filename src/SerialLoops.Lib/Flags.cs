using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib
{
    public class Flags
    {
        public const int NUM_FLAGS = 5120;
        public static readonly Dictionary<int, string> _names = JsonSerializer.Deserialize<Dictionary<int, string>>(File.ReadAllText(Extensions.GetLocalizedFilePath(Path.Combine("Defaults", "DefaultFlags"), "json")));

        public static string GetFlagNickname(int flag, Project project)
        {
            if (_names.TryGetValue(flag, out string value))
            {
                return value;
            }

            TopicItem topic = (TopicItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).TopicEntry.Id == flag);
            if (topic is not null)
            {
                return string.Format(project.Localize("{0} Obtained"), topic.DisplayName);
            }

            TopicItem readTopic = (TopicItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic && 
                ((((TopicItem)i).TopicEntry.Type == TopicType.Main && ((TopicItem)i).HiddenMainTopic is not null && ((TopicItem)i).TopicEntry.Id + 3463 == flag) ||
                (((TopicItem)i).TopicEntry.Type != TopicType.Main && ((TopicItem)i).TopicEntry.Id + 3451 == flag)));
            if (readTopic is not null)
            {
                return string.Format(project.Localize("{0} Watched in Extras"), readTopic.DisplayName);
            }

            BackgroundMusicItem bgm = (BackgroundMusicItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.BGM && ((BackgroundMusicItem)i).Flag == flag);
            if (bgm is not null)
            {
                return string.Format(project.Localize("Listened to {0}"), bgm.DisplayName);
            }

            BackgroundItem bg = (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Flag == flag);
            if (bg is not null && flag > 0)
            {
                return string.Format(project.Localize("{0} ({1}) Seen"), bg.CgName, bg.DisplayName);
            }

            GroupSelectionItem groupSelection = (GroupSelectionItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Group_Selection && 
                ((GroupSelectionItem)i).Selection.Activities.Any(a => a?.Routes.Any(r => r?.Flag == flag) ?? false));
            if (groupSelection is not null)
            {
                ScenarioRoute route = groupSelection.Selection.Activities.First(a => a?.Routes.Any(r => r.Flag == flag) ?? false).Routes.First(r => r.Flag == flag);
                return string.Format(project.Localize("Route \"{0}\" Completed"), route.Title.GetSubstitutedString(project));
            }

            ScriptItem script = (ScriptItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Script &&
                flag >= ((ScriptItem)i).StartReadFlag && flag < ((ScriptItem)i).StartReadFlag + ((ScriptItem)i).Event.ScriptSections.Count);
            if (script is not null)
            {
                return string.Format(project.Localize("Script {0} Section {1} Completed"), script.DisplayName, script.Event.ScriptSections[flag - script.StartReadFlag].Name);
            }

            return $"F{flag:D2}";
        }
    }
}
