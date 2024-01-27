using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib
{
    public class Flags
    {
        private static readonly Dictionary<int, string> _names = new()
        {
            { 4231, "Unlock Chess Mode at Next Checkpoint Save" },
            { 4311, "Haruhi Ending Unlocked" },
            { 4312, "Mikuru Ending Unlocked" },
            { 4313, "Nagato Ending Unlocked" },
            { 4314, "Koizumi Ending Unlocked" },
            { 4315, "Tsuruya Ending Unlocked" },
            { 4378, "Unlock Episode 1 New Game at Next Checkpoint Save" },
            { 4379, "Unlock Episode 2 New Game at Next Checkpoint Save" },
            { 4380, "Unlock Episode 3 New Game at Next Checkpoint Save" },
            { 4381, "Unlock Episode 4 New Game at Next Checkpoint Save" },
            { 4382, "Unlock Episode 5 New Game at Next Checkpoint Save" },
            { 4383, "Haruhi Ending Accessible" },
            { 4384, "Unlock Extra Mode at Next Checkpoint Save" },
            { 4628, "Unlock Mystery Girl Voice at Next Checkpoint Save" },
            { 4629, "Episode 1 Unlocked" },
            { 4630, "Episode 2 Unlocked" },
            { 4631, "Episode 3 Unlocked" },
            { 4632, "Episode 4 Unlocked" },
            { 4633, "Episode 5 Unlocked" },
            { 4634, "Extra Mode Unlocked" },
            { 4635, "Chess Mode Unlocked" },
            { 4636, "Mystery Girl Voice Unlocked" },
        };

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
