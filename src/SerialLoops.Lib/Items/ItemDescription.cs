using System;
using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Lib.Items
{
    public partial class ItemDescription : ReactiveObject
    {
        [Reactive]
        public string Name { get; set; }
        [Reactive]
        public bool CanRename { get; set; }
        [Reactive]
        public string DisplayName { get; set; }
        public ItemType Type { get; private set; }
        [Reactive]
        public bool UnsavedChanges { get; set; }

        public ItemDescription(string name, ItemType type, string displayName)
        {
            Name = name;
            Type = type;
            CanRename = true;
            if (!string.IsNullOrEmpty(displayName))
            {
                DisplayName = displayName;
            }
            else
            {
                DisplayName = Name;
            }
        }

        public void Rename(string newName)
        {
            DisplayName = newName;
        }

        // Enum with values for each type of item
        public enum ItemType
        {
            Background,
            BGM,
            Character,
            Character_Sprite,
            Chess_Puzzle,
            Chibi,
            Group_Selection,
            Item,
            Layout,
            Map,
            Place,
            Puzzle,
            Scenario,
            Script,
            SFX,
            System_Texture,
            Topic,
            Transition,
            Voice,
        }

        public List<ItemDescription> GetReferencesTo(Project project)
        {
            List<ItemDescription> references = [];
            ScenarioItem scenario = (ScenarioItem)project.Items.First(i => i.Name == "Scenario");
            switch (Type)
            {
                case ItemType.Background:
                    BackgroundItem bg = (BackgroundItem)this;
                    string[] bgCommands =
                    [
                        EventFile.CommandVerb.KBG_DISP.ToString(),
                        EventFile.CommandVerb.BG_DISP.ToString(),
                        EventFile.CommandVerb.BG_DISP2.ToString(),
                        EventFile.CommandVerb.BG_DISPCG.ToString(),
                        EventFile.CommandVerb.BG_FADE.ToString(),
                    ];
                    (string ScriptName, ScriptCommandInvocation command)[] bgScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => bgCommands.Contains(c.Command.Mnemonic)).Select(c => (e.Name[0..^1], c))))
                        .Where(t => t.c.Parameters[0] == bg.Id || t.c.Command.Mnemonic == EventFile.CommandVerb.BG_FADE.ToString() && t.c.Parameters[1] == bg.Id).ToArray();
                    return project.Items.Where(i => bgScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();

                case ItemType.BGM:
                    BackgroundMusicItem bgm = (BackgroundMusicItem)this;
                    (string ScriptName, ScriptCommandInvocation comamnd)[] bgmScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.BGM_PLAY.ToString()).Select(c => (e.Name[0..^1], c))))
                        .Where(t => t.c.Parameters[0] == bgm.Index).ToArray();
                    return project.Items.Where(i => bgmScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();

                case ItemType.Character:
                    CharacterItem character = (CharacterItem)this;
                    return project.Items.Where(i => i.Type == ItemType.Script && ((ScriptItem)i).Event.DialogueSection.Objects.Any(l => l.Speaker == character.MessageInfo.Character)).ToList();

                case ItemType.Character_Sprite:
                    CharacterSpriteItem sprite = (CharacterSpriteItem)this;
                    (string ScriptName, ScriptCommandInvocation command)[] spriteScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.DIALOGUE.ToString()).Select(c => (e.Name[0..^1], c))))
                        .Where(t => t.c.Parameters[1] == sprite.Index).ToArray();
                    return project.Items.Where(i => spriteScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();

                case ItemType.Chibi:
                    ChibiItem chibi = (ChibiItem)this;
                    references.AddRange(project.Items.Where(i => i.Type == ItemType.Script && project.Evt.Files.Where(e =>
                        e.MapCharactersSection?.Objects?.Any(t => t.CharacterIndex == chibi.ChibiIndex) ?? false).Select(e => e.Index).Contains(((ScriptItem)i).Event.Index)));
                    (string ScriptName, ScriptCommandInvocation command)[] chibiScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.CHIBI_ENTEREXIT.ToString()).Select(c => (e.Name[0..^1], c))))
                        .Where(t => t.c.Parameters[0] == chibi.TopScreenIndex).ToArray();
                    references.AddRange(project.Items.Where(i => chibiScriptUses.Select(s => s.ScriptName).Contains(i.Name)));
                    return references.Distinct().ToList();

                case ItemType.Group_Selection:
                    GroupSelectionItem groupSelection = (GroupSelectionItem)this;
                    if (scenario.Scenario.Commands.Any(c => c.Verb == ScenarioCommand.ScenarioVerb.ROUTE_SELECT && c.Parameter == groupSelection.Index))
                    {
                        references.Add(scenario);
                    }
                    return references;

                case ItemType.Map:
                    MapItem map = (MapItem)this;
                    (string ScriptName, ScriptCommandInvocation command)[] mapScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.LOAD_ISOMAP.ToString()).Select(c => (e.Name[0..^1], c))))
                        .Where(t => t.c.Parameters[0] == map.Map.Index).ToArray();
                    return project.Items.Where(i => i.Type == ItemType.Puzzle && ((PuzzleItem)i).Puzzle.Settings.MapId == map.QmapIndex)
                        .Concat(project.Items.Where(i => mapScriptUses.Select(s => s.ScriptName).Contains(i.Name))).ToList();

                case ItemType.Place:
                    PlaceItem place = (PlaceItem)this;
                    (string ScriptName, ScriptCommandInvocation command)[] placeScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.SET_PLACE.ToString()).Select(c => (e.Name[0..^1], c))))
                        .Where(t => t.c.Parameters[1] == place.Index).ToArray();
                    return project.Items.Where(i => placeScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();

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
                    references.AddRange(project.Items.Where(i => i.Type == ItemType.Group_Selection && ((GroupSelectionItem)i).Selection.Activities.Where(s => s is not null).Any(s => s.Routes.Any(r => r.ScriptIndex == script.Event.Index))));
                    references.AddRange(project.Items.Where(i => i.Type == ItemType.Topic &&
                        (((TopicItem)i).TopicEntry.CardType != TopicCardType.Main && ((TopicItem)i).TopicEntry.EventIndex == script.Event.Index ||
                        (((TopicItem)i).HiddenMainTopic?.EventIndex ?? -1) == script.Event.Index)));
                    references.AddRange(project.Items.Where(i => i.Type == ItemType.Script && ((ScriptItem)i).Event.ConditionalsSection.Objects.Contains(Name)));
                    return references;

                case ItemType.SFX:
                    SfxItem sfx = (SfxItem)this;
                    (string ScriptName, ScriptCommandInvocation command)[] sfxScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.SND_PLAY.ToString()).Select(c => (e.Name[0..^1], c))))
                        .Where(t => t.c.Parameters[0] == sfx.Index).ToArray();
                    references.AddRange(project.Items.Where(i => sfxScriptUses.Select(s => s.ScriptName).Contains(i.Name)));
                    references.AddRange(project.Items.Where(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.VoiceFont == sfx.Index));
                    return references;

                case ItemType.Topic:
                    TopicItem topic = (TopicItem)this;
                    (string ScriptName, ScriptCommandInvocation command)[] topicScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.TOPIC_GET.ToString()).Select(c => (e.Name[0..^1], c))))
                        .Where(t => t.c.Parameters[0] == topic.TopicEntry.Id).ToArray();
                    return project.Items.Where(i => topicScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();

                case ItemType.Voice:
                    VoicedLineItem voicedLine = (VoicedLineItem)this;
                    (string ScriptName, ScriptCommandInvocation command)[] vceScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.DIALOGUE.ToString()).Select(c => (e.Name[0..^1], c))))
                            .Where(t => t.c.Parameters[5] == voicedLine.Index)
                            .Concat(project.Evt.Files.AsParallel().SelectMany(e =>
                            e.ScriptSections.SelectMany(sec =>
                                sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.VCE_PLAY.ToString()).Select(c => (e.Name[0..^1], c))))
                            .Where(t => t.c.Parameters[0] == voicedLine.Index))
                            .ToArray();
                    return project.Items.Where(i => vceScriptUses.Select(s => s.ScriptName).Contains(i.Name)).ToList();

                default:
                    return references;
            }
        }
    }
}
