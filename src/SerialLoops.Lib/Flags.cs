using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SerialLoops.Lib
{
    public class Flags
    {
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
                return $"{topic.DisplayName} Obtained";
            }

            TopicItem readTopic = (TopicItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic && 
                ((((TopicItem)i).TopicEntry.Type == TopicType.Main && ((TopicItem)i).HiddenMainTopic is not null && ((TopicItem)i).TopicEntry.Id + 3463 == flag) ||
                (((TopicItem)i).TopicEntry.Type != TopicType.Main && ((TopicItem)i).TopicEntry.Id + 3451 == flag)));
            if (readTopic is not null)
            {
                return $"{readTopic.DisplayName} Watched in Extras";
            }

            BackgroundMusicItem bgm = (BackgroundMusicItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.BGM && ((BackgroundMusicItem)i).Flag == flag);
            if (bgm is not null)
            {
                return $"Listened to {bgm.DisplayName}";
            }

            BackgroundItem bg = (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Flag == flag);
            if (bg is not null && flag > 0)
            {
                return $"{bg.CgName} ({bg.DisplayName}) Seen";
            }

            GroupSelectionItem groupSelection = (GroupSelectionItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Group_Selection && 
                ((GroupSelectionItem)i).Selection.Activities.Any(a => a?.Routes.Any(r => r?.Flag == flag) ?? false));
            if (groupSelection is not null)
            {
                ScenarioRoute route = groupSelection.Selection.Activities.First(a => a?.Routes.Any(r => r.Flag == flag) ?? false).Routes.First(r => r.Flag == flag);
                return $"Route \"{route.Title.GetSubstitutedString(project)}\" Completed";
            }

            return $"F{flag:D2}";
        }
    }
}
