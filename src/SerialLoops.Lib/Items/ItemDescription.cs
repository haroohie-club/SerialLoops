using HaruhiChokuretsuLib.Archive.Event;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class ItemDescription
    {

        public string Name { get; protected set; }
        public string DisplayName { get; protected set; }
        public string DisplayNameWithStatus => UnsavedChanges ? $"{DisplayName} *" : DisplayName;
        public ItemType Type { get; private set; }
        public bool UnsavedChanges { get; set; } = false;

        public ItemDescription(string name, ItemType type, string displayName)
        {
            Name = name;
            Type = type;
            if (!string.IsNullOrEmpty(displayName))
            {
                DisplayName = displayName;
            }
            else
            {
                DisplayName = Name;
            }
        }

        // Enum with values for each type of item
        public enum ItemType
        {
            Background,
            BGM,
            Character_Sprite,
            Chess,
            Chibi,
            Dialogue_Config,
            Group_Selection,
            Map,
            Puzzle,
            Scenario,
            Script,
            Sfx,
            System_Texture,
            Topic,
            Transition,
            Tutorial,
            Voice,
        }

        public List<ItemDescription> GetReferencesTo(Project project)
        {
            List<ItemDescription> references = new();
            ScenarioItem scenario = (ScenarioItem)project.Items.First(i => i.Name == "Scenario");
            switch (Type)
            {
                case ItemType.Background:
                    BackgroundItem bg = (BackgroundItem)this;
                    return project.Items.Where(i => bg.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                case ItemType.BGM:
                    BackgroundMusicItem bgm = (BackgroundMusicItem)this;
                    return project.Items.Where(i => bgm.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                case ItemType.Character_Sprite:
                    CharacterSpriteItem sprite = (CharacterSpriteItem)this;
                    return project.Items.Where(i => sprite.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                case ItemType.Chibi:
                    ChibiItem chibi = (ChibiItem)this;
                    return project.Items.Where(i => chibi.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                case ItemType.Puzzle:
                    PuzzleItem puzzle = (PuzzleItem)this;
                    if (scenario.Scenario.Commands.Any(c => c.Verb == ScenarioCommand.ScenarioVerb.PUZZLE_PHASE && c.Parameter == puzzle.Puzzle.Index))
                    {
                        references.Add(scenario);
                    }
                    return references;
                case ItemType.Script:
                    ScriptItem script = (ScriptItem)this;
                    if (scenario.Scenario.Commands.Any(c => c.Verb == ScenarioCommand.ScenarioVerb.LOAD_SCENE && c.Parameter == script.Event.Index))
                    {
                        references.Add(scenario);
                    }
                    references.AddRange(project.Items.Where(i => i.Type == ItemType.Topic && ((TopicItem)i).Topic.EventIndex == script.Event.Index));
                    return references;
                case ItemType.Topic:
                    TopicItem topic = (TopicItem)this;
                    return project.Items.Where(i => topic.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                case ItemType.Voice:
                    VoicedLineItem voicedLine = (VoicedLineItem)this;
                    return project.Items.Where(i => voicedLine.ScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();
                default:
                    return references;
            }
        }

        public List<ItemDescription> GetReferencedBy(Project project)
        {
            List<ItemDescription> references = new();

            switch (Type)
            {
                default:
                    return references;
            }
        }
    }
}
