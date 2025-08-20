using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib;

public class Flags
{
    public const int NUM_FLAGS = 5120;
    public static readonly Dictionary<int, string> _names = JsonSerializer.Deserialize<Dictionary<int, string>>(File.ReadAllText(Extensions.GetLocalizedFilePath(Path.Combine("Defaults", "DefaultFlags"), "json")));

    public static string GetFlagNickname(int flag, Project project)
    {
        if (_names.TryGetValue(flag + 1, out string value))
        {
            return $"{value} (F{flag:D2})";
        }

        TopicItem topic = (TopicItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).TopicEntry.Id == flag + 1);
        if (topic is not null)
        {
            return string.Format(project.Localize("FlagTopicObtained"), topic.DisplayName, flag);
        }

        TopicItem readTopic = (TopicItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic &&
                                                                           ((((TopicItem)i).TopicEntry.Type == TopicType.Main && ((TopicItem)i).HiddenMainTopic is not null && ((TopicItem)i).TopicEntry.Id + 3463 == flag + 1) ||
                                                                            (((TopicItem)i).TopicEntry.Type != TopicType.Main && ((TopicItem)i).TopicEntry.Id + 3451 == flag + 1)));
        if (readTopic is not null)
        {
            return string.Format(project.Localize("FlagTopicSceneWatchedInExtras"), readTopic.DisplayName, flag);
        }

        BackgroundMusicItem bgm = (BackgroundMusicItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.BGM && ((BackgroundMusicItem)i).Flag == flag + 1);
        if (bgm is not null)
        {
            return string.Format(project.Localize("FlagListenedToBgm"), bgm.DisplayName, flag);
        }

        BackgroundItem bg = (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Flag == flag + 1);
        if (bg is not null && flag + 1 > 0)
        {
            return string.Format(project.Localize("FlagCgSeen"), bg.CgName, bg.DisplayName, flag);
        }

        GroupSelectionItem groupSelection = (GroupSelectionItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Group_Selection &&
            ((GroupSelectionItem)i).Selection.Activities.Any(a => a?.Routes.Any(r => r?.Flag == flag + 1) ?? false));
        if (groupSelection is not null)
        {
            ScenarioRoute route = groupSelection.Selection.Activities.First(a => a?.Routes.Any(r => r.Flag == flag + 1) ?? false).Routes.First(r => r.Flag == flag + 1);
            return string.Format(project.Localize("FlagsRouteCompletedDescription"), route.Title.GetSubstitutedString(project), flag);
        }

        ScriptItem script = (ScriptItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).StartReadFlag > 0 &&
                                                                          flag + 1 >= ((ScriptItem)i).StartReadFlag && flag + 1 < ((ScriptItem)i).StartReadFlag + ((ScriptItem)i).Event.ScriptSections.Count);
        if (script is not null)
        {
            return string.Format(project.Localize("Script {0} Section {1} Completed (F{2:D2})"), script.DisplayName, script.Event.ScriptSections[flag + 1 - script.StartReadFlag].Name, flag);
        }

        Tutorial tutorial = project.TutorialFile.Tutorials.FirstOrDefault(t => t.Id != 0 && t.Id == flag + 1);
        if (tutorial is not null)
        {
            return string.Format(project.Localize("FlagsTutorialCompletedDescription"),
                ((ScriptItem)project.Items.First(i =>
                    i.Type == ItemDescription.ItemType.Script &&
                    ((ScriptItem)i).Event.Index == tutorial.AssociatedScript)).DisplayName, flag);
        }

        return $"F{flag:D2}";
    }
}
