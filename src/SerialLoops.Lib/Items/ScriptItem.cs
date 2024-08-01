using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using QuikGraph;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SkiaSharp;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.Lib.Items
{
    public class ScriptItem : Item
    {
        public EventFile Event { get; set; }
        public short StartReadFlag { get; set; }
        public short SfxGroupIndex { get; set; }
        public AdjacencyGraph<ScriptSection, ScriptSectionEdge> Graph { get; set; } = new();
        private readonly Func<string, string> _localize;

        public ScriptItem(string name) : base(name, ItemType.Script)
        {
        }
        public ScriptItem(EventFile evt, EventTable evtTbl, Func<string, string> localize, ILogger log) : base(evt.Name[0..^1], ItemType.Script)
        {
            Event = evt;
            UpdateEventTableInfo(evtTbl);
            _localize = localize;

            PruneLabelsSection(log);
            Graph.AddVertexRange(Event.ScriptSections);
        }

        public Dictionary<ScriptSection, List<ScriptItemCommand>> GetScriptCommandTree(Project project, ILogger log)
        {
            try
            {
                Dictionary<ScriptSection, List<ScriptItemCommand>> commands = [];
                foreach (ScriptSection section in Event.ScriptSections)
                {
                    commands.Add(section, []);
                    foreach (ScriptCommandInvocation command in section.Objects)
                    {
                        commands[section].Add(ScriptItemCommand.FromInvocation(command, section, commands[section].Count, Event, project, _localize, log));
                    }
                }
                return commands;
            }
            catch (Exception ex)
            {
                log.LogException(string.Format(project.Localize("Error getting script command tree for script {0} ({1})"), DisplayName, Name), ex);
                return null;
            }
        }

        public void CalculateGraphEdges(Dictionary<ScriptSection, List<ScriptItemCommand>> commandTree, ILogger log)
        {
            try
            {
                foreach (ScriptSection section in commandTree.Keys)
                {
                    bool @continue = false;
                    foreach (ScriptItemCommand command in commandTree[section])
                    {
                        if (command.Verb == CommandVerb.INVEST_START)
                        {
                            Graph.AddEdge(new() { Source = section, Target = ((ScriptSectionScriptParameter)command.Parameters[4]).Section });
                            Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                                Event.LabelsSection.Objects.Where(l =>
                                Event.MapCharactersSection?.Objects.Select(c => c.TalkScriptBlock).Contains(l.Id) ?? false)
                                .Select(l => l.Name.Replace("/", "")).Contains(s.Name)).Select(s => new ScriptSectionEdge() { Source = section, Target = s }));
                            Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                                Event.LabelsSection.Objects.Where(l =>
                                Event.InteractableObjectsSection.Objects.Select(o => o.ScriptBlock).Contains(l.Id))
                                .Select(l => l.Name.Replace("/", "")).Contains(s.Name)).Select(s => new ScriptSectionEdge() { Source = section, Target = s }));
                            @continue = true;
                        }
                        else if (command.Verb == CommandVerb.GOTO)
                        {
                            try
                            {
                                Graph.AddEdge(new() { Source = section, Target = ((ScriptSectionScriptParameter)command.Parameters[0]).Section });
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                log.LogWarning("Failed to add graph edge for GOTO command as script section parameter was out of range.");
                            }
                            @continue = true;
                        }
                        else if (command.Verb == CommandVerb.VGOTO)
                        {
                            try
                            {
                                Graph.AddEdge(new() { Source = section, Target = ((ScriptSectionScriptParameter)command.Parameters[1]).Section });
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                log.LogWarning("Failed to add graph edge for VGOTO command as script section parameter was out of range.");
                            }
                        }
                        else if (command.Verb == CommandVerb.CHESS_VGOTO)
                        {
                            Graph.AddEdgeRange(command.Parameters.Cast<ScriptSectionScriptParameter>()
                                .Where(p => p.Section is not null).Select(p => new ScriptSectionEdge() { Source = section, Target = p.Section }));
                            ScriptSection miss2Section = Event.ScriptSections.FirstOrDefault(s => s.Name == "NONEMiss2");
                            if (miss2Section is not null)
                            {
                                Graph.AddEdge(new() { Source = section, Target = Event.ScriptSections.First(s => s.Name == "NONEMiss2") }); // hardcode this section, even tho you can't get to it
                            }
                        }
                        else if (command.Verb == CommandVerb.SELECT)
                        {
                            Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                                Event.LabelsSection.Objects.Where(l =>
                                command.Parameters.Where(p => p.Type == ScriptParameter.ParameterType.OPTION).Cast<OptionScriptParameter>()
                                .Where(p => p.Option.Id > 0).Select(p => p.Option.Id).Contains(l.Id)).Select(l => l.Name.Replace("/", "")).Contains(s.Name))
                                .Select(s => new ScriptSectionEdge() { Source = section, Target = s }));
                            @continue = true;
                        }
                        else if (command.Verb == CommandVerb.NEXT_SCENE)
                        {
                            @continue = true;
                        }
                        else if (command.Verb == CommandVerb.BACK && section.Name != "SCRIPT00")
                        {
                            @continue = true;
                        }
                        else if (Name.StartsWith("CHS") && Name.EndsWith("90") && commandTree.Keys.ToList().IndexOf(section) > 1 && command.Index == 0)
                        {
                            Graph.AddEdge(new() { Source = Event.ScriptSections[1], Target = section }); // these particular chess files have no VGOTOs, so uh... we manually hardcode them
                        }
                    }
                    if (@continue)
                    {
                        continue;
                    }
                    if (section != commandTree.Keys.Last())
                    {
                        Graph.AddEdge(new() { Source = section, Target = commandTree.Keys.ElementAt(commandTree.Keys.ToList().IndexOf(section) + 1) });
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogException("Failed to calculate graph edges!", ex);
                log.Log($"Script: {Name}, DisplayName: {DisplayName}");
            }
        }

        public ScriptPreview GetScriptPreview(Dictionary<ScriptSection, List<ScriptItemCommand>> commandTree, ScriptItemCommand currentCommand, Project project, ILogger log)
        {
            ScriptPreview preview = new();

            if (currentCommand is not null)
            {
                List<ScriptItemCommand> commands = currentCommand.WalkCommandGraph(commandTree, Graph);

                if (commands is null)
                {
                    log.LogWarning($"No path found to current command.");
                    preview.ErrorImage = "SerialLoops.Graphics.ScriptPreviewError.png";
                    return preview;
                }

                if (commands.Any(c => c.Verb == CommandVerb.EPHEADER)
                    && ((EpisodeHeaderScriptParameter)commands.Last(c => c.Verb == CommandVerb.EPHEADER).Parameters[0])
                        .EpisodeHeaderIndex != EpisodeHeaderScriptParameter.Episode.None)
                {
                    preview.EpisodeHeader = (short)((EpisodeHeaderScriptParameter)commands.Last(c => c.Verb == CommandVerb.EPHEADER).Parameters[0]).EpisodeHeaderIndex;
                }

                // Draw top screen "kinetic" background
                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    if (commands[i].Verb == CommandVerb.KBG_DISP && ((BgScriptParameter)commands[i].Parameters[0]).Background is not null)
                    {
                        preview.Kbg = ((BgScriptParameter)commands[i].Parameters[0]).Background;
                        break;
                    }
                }

                // Draw Place
                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    if (commands[i].Verb == CommandVerb.SET_PLACE)
                    {
                        if (((BoolScriptParameter)commands[i].Parameters[0]).Value && (((PlaceScriptParameter)commands[i].Parameters[1]).Place is not null))
                        {
                            preview.Place = ((PlaceScriptParameter)commands[i].Parameters[1]).Place;
                        }
                        break;
                    }
                }

                // Draw top screen chibis
                List<ChibiItem> chibis = [];

                foreach (StartingChibiEntry chibi in Event.StartingChibisSection?.Objects ?? [])
                {
                    if (chibi.ChibiIndex > 0)
                    {
                        chibis.Add((ChibiItem)project.Items.First(i => i.Type == ItemType.Chibi && ((ChibiItem)i).TopScreenIndex == chibi.ChibiIndex));
                    }
                }
                for (int i = 0; i < commands.Count; i++)
                {
                    if (commands[i].Verb == CommandVerb.OP_MODE)
                    {
                        // Kyon auto-added by OP_MODE command
                        ChibiItem chibi = (ChibiItem)project.Items.First(i => i.Type == ItemType.Chibi && ((ChibiItem)i).TopScreenIndex == 1);
                        if (!chibis.Contains(chibi))
                        {
                            chibis.Add(chibi);
                        }
                    }
                    if (commands[i].Verb == CommandVerb.CHIBI_ENTEREXIT)
                    {
                        if (((ChibiEnterExitScriptParameter)commands[i].Parameters[1]).Mode == ChibiEnterExitScriptParameter.ChibiEnterExitType.ENTER)
                        {
                            ChibiItem chibi = ((ChibiScriptParameter)commands[i].Parameters[0]).Chibi;
                            if (!chibis.Contains(chibi))
                            {
                                if (chibi.TopScreenIndex < 1 || chibis.Count == 0)
                                {
                                    chibis.Add(chibi);
                                }
                                else
                                {
                                    bool inserted = false;
                                    for (int j = 0; j < chibis.Count; j++)
                                    {
                                        if (chibis[j].TopScreenIndex > chibi.TopScreenIndex)
                                        {
                                            chibis.Insert(j, chibi);
                                            inserted = true;
                                            break;
                                        }
                                    }
                                    if (!inserted)
                                    {
                                        chibis.Add(chibi);
                                    }
                                }
                            }
                            else
                            {
                                log.LogWarning($"Chibi {chibi.Name} set to join, but already was present");
                            }
                        }
                        else
                        {
                            try
                            {
                                chibis.Remove(((ChibiScriptParameter)commands[i].Parameters[0]).Chibi);
                            }
                            catch (Exception)
                            {
                                log.LogWarning($"Chibi set to leave was not present.");
                            }
                        }
                    }
                }

                int chibiStartX, chibiY;
                if (commands.Any(c => c.Verb == CommandVerb.OP_MODE))
                {
                    chibiStartX = 100;
                    chibiY = 50;
                }
                else
                {
                    chibiStartX = 80;
                    chibiY = 100;
                }
                int chibiCurrentX = chibiStartX;
                int chibiWidth = 0;
                foreach (ChibiItem chibi in chibis)
                {
                    SKBitmap chibiFrame = chibi.ChibiAnimations.First().Value.ElementAt(0).Frame;
                    preview.TopScreenChibis.Add((chibi, chibiCurrentX, chibiY));
                    chibiWidth = chibiFrame.Width - 16;
                    if (chibiY == 50)
                    {
                        chibiY = 100;
                        chibiCurrentX = 80;
                    }
                    else
                    {
                        chibiCurrentX += chibiWidth;
                    }
                }

                // Draw top screen chibi emotes
                if (currentCommand.Verb == CommandVerb.CHIBI_EMOTE)
                {
                    ChibiItem chibi = ((ChibiScriptParameter)currentCommand.Parameters[0]).Chibi;
                    if (chibis.Contains(chibi))
                    {
                        int chibiIndex = chibis.IndexOf(chibi);
                        int internalYOffset = ((int)((ChibiEmoteScriptParameter)currentCommand.Parameters[1]).Emote - 1) * 32;
                        int externalXOffset = chibiStartX + chibiWidth * chibiIndex;
                        preview.ChibiEmote = (internalYOffset, externalXOffset, chibi);
                    }
                    else
                    {
                        log.LogWarning($"Chibi {chibi.Name} not currently on screen; cannot display emote.");
                    }
                }

                // Draw background
                bool bgReverted = false;
                ScriptItemCommand palCommand = commands.LastOrDefault(c => c.Verb == CommandVerb.PALEFFECT);
                ScriptItemCommand lastBgCommand = commands.LastOrDefault(c => c.Verb == CommandVerb.BG_DISP ||
                    c.Verb == CommandVerb.BG_DISP2 || c.Verb == CommandVerb.BG_DISPCG || c.Verb == CommandVerb.BG_FADE ||
                    c.Verb == CommandVerb.BG_REVERT);
                SKPaint palEffectPaint = PaletteEffectScriptParameter.IdentityPaint;
                if (palCommand is not null && lastBgCommand is not null && commands.IndexOf(palCommand) > commands.IndexOf(lastBgCommand))
                {
                    preview.BgPalEffect = ((PaletteEffectScriptParameter)palCommand.Parameters[0]).Effect;
                }

                ScriptItemCommand bgScrollCommand = null;
                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    if (commands[i].Verb == CommandVerb.BG_REVERT)
                    {
                        bgReverted = true;
                        continue;
                    }
                    if (commands[i].Verb == CommandVerb.BG_SCROLL)
                    {
                        bgScrollCommand = commands[i];
                        continue;
                    }
                    // Checks to see if this is one of the commands that sets a BG_REVERT immune background or if BG_REVERT hasn't been called
                    if (commands[i].Verb == CommandVerb.BG_DISP || commands[i].Verb == CommandVerb.BG_DISP2 ||
                        (commands[i].Verb == CommandVerb.BG_FADE && (((BgScriptParameter)commands[i].Parameters[0]).Background is not null)) ||
                        (!bgReverted && (commands[i].Verb == CommandVerb.BG_DISPCG || commands[i].Verb == CommandVerb.BG_FADE)))
                    {
                        BackgroundItem background = (commands[i].Verb == CommandVerb.BG_FADE && ((BgScriptParameter)commands[i].Parameters[0]).Background is null) ?
                            ((BgScriptParameter)commands[i].Parameters[1]).Background : ((BgScriptParameter)commands[i].Parameters[0]).Background;

                        if (background is not null)
                        {
                            preview.Background = background;
                            preview.BgScrollCommand = bgScrollCommand;
                            if (commands[i].Parameters.Count >= 2 && commands[i].Parameters[1].Type == ScriptParameter.ParameterType.BOOL)
                            {
                                preview.BgPositionBool = ((BoolScriptParameter)commands[i].Parameters[1]).Value;
                            }
                            break;
                        }
                    }
                }

                // Draw items
                ScriptItemCommand lastItemCommand = commands.LastOrDefault(c => c.Verb == CommandVerb.ITEM_DISPIMG);
                if (lastItemCommand is not null)
                {
                    ItemItem item = (ItemItem)project.Items.FirstOrDefault(i => i.Type == ItemType.Item && ((ItemScriptParameter)lastItemCommand.Parameters[0]).ItemIndex == ((ItemItem)i).ItemIndex);
                    if (item is not null && ((ItemLocationScriptParameter)lastItemCommand.Parameters[1]).Location != ItemItem.ItemLocation.Exit)
                    {
                        preview.Item = (item, ((ItemLocationScriptParameter)lastItemCommand.Parameters[1]).Location);
                    }
                }

                // Draw character sprites
                Dictionary<CharacterItem, PositionedSprite> sprites = [];
                Dictionary<CharacterItem, PositionedSprite> previousSprites = [];

                CharacterItem previousCharacter = null;
                ScriptItemCommand previousCommand = null;
                foreach (ScriptItemCommand command in commands)
                {
                    if (previousCommand?.Verb == CommandVerb.DIALOGUE)
                    {
                        SpriteExitScriptParameter spriteExitMoveParam = (SpriteExitScriptParameter)previousCommand?.Parameters[3]; // exits/moves happen _after_ dialogue is advanced, so we check these at this point
                        if (spriteExitMoveParam.ExitTransition != SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT)
                        {
                            CharacterItem prevCharacter = (CharacterItem)project.Items.First(i => i.Type == ItemType.Character &&
                                i.Name == $"CHR_{project.Characters[(int)((DialogueScriptParameter)previousCommand.Parameters[0]).Line.Speaker].Name}");
                            SpriteScriptParameter previousSpriteParam = (SpriteScriptParameter)previousCommand.Parameters[1];
                            short layer = ((ShortScriptParameter)previousCommand.Parameters[9]).Value;
                            switch (spriteExitMoveParam.ExitTransition)
                            {
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_LEFT_FADE_OUT:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_RIGHT_FADE_OUT:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT:
                                case SpriteExitScriptParameter.SpriteExitTransition.FADE_OUT_CENTER:
                                case SpriteExitScriptParameter.SpriteExitTransition.FADE_OUT_LEFT:
                                    if (sprites.ContainsKey(prevCharacter) && previousSprites.ContainsKey(prevCharacter) && ((SpriteScriptParameter)previousCommand.Parameters[1]).Sprite?.Sprite?.Character == prevCharacter.MessageInfo.Character)
                                    {
                                        sprites.Remove(prevCharacter);
                                        previousSprites.Remove(prevCharacter);
                                    }
                                    break;

                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_CENTER_TO_LEFT_AND_STAY:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_RIGHT_TO_LEFT_AND_STAY:
                                    sprites[prevCharacter] = new() { Sprite = previousSpriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.LEFT.GetSpriteX(), Layer = layer } };
                                    break;

                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_CENTER_TO_RIGHT_AND_STAY:
                                case SpriteExitScriptParameter.SpriteExitTransition.SLIDE_LEFT_TO_RIGHT_AND_STAY:
                                    sprites[prevCharacter] = new() { Sprite = previousSpriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.RIGHT.GetSpriteX(), Layer = layer } };
                                    break;
                            }
                        }
                    }
                    if (command.Verb == CommandVerb.DIALOGUE)
                    {
                        SpriteScriptParameter spriteParam = (SpriteScriptParameter)command.Parameters[1];
                        SKPaint spritePaint = PaletteEffectScriptParameter.IdentityPaint;
                        if (commands.IndexOf(palCommand) > commands.IndexOf(command))
                        {
                            spritePaint = ((PaletteEffectScriptParameter)palCommand.Parameters[0]).Effect switch
                            {
                                PaletteEffectScriptParameter.PaletteEffect.INVERTED => PaletteEffectScriptParameter.InvertedPaint,
                                PaletteEffectScriptParameter.PaletteEffect.GRAYSCALE => PaletteEffectScriptParameter.GrayscalePaint,
                                PaletteEffectScriptParameter.PaletteEffect.SEPIA => PaletteEffectScriptParameter.SepiaPaint,
                                PaletteEffectScriptParameter.PaletteEffect.DIMMED => PaletteEffectScriptParameter.DimmedPaint,
                                _ => PaletteEffectScriptParameter.IdentityPaint,
                            };
                        }
                        if (spriteParam.Sprite is not null)
                        {
                            CharacterItem character;
                            try
                            {
                                character = (CharacterItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Character &&
                                i.DisplayName == $"CHR_{project.Characters[(int)((DialogueScriptParameter)command.Parameters[0]).Line.Speaker].Name}");
                            }
                            catch (InvalidOperationException)
                            {
                                log.LogWarning($"Unable to determine speaking character in DIALOGUE command in {DisplayName}.");
                                preview.ErrorImage = "SerialLoops.Graphics.ScriptPreviewError.png";
                                return preview;
                            }
                            SpriteEntranceScriptParameter spriteEntranceParam = (SpriteEntranceScriptParameter)command.Parameters[2];
                            SpriteShakeScriptParameter spriteShakeParam = (SpriteShakeScriptParameter)command.Parameters[4];
                            short layer = ((ShortScriptParameter)command.Parameters[9]).Value;

                            bool spriteIsNew = !sprites.ContainsKey(character);
                            if (spriteIsNew && spriteEntranceParam.EntranceTransition != SpriteEntranceScriptParameter.SpriteEntranceTransition.NO_TRANSITION)
                            {
                                sprites.Add(character, new());
                                previousSprites.Add(character, new());
                            }
                            if (sprites.ContainsKey(character))
                            {
                                previousSprites[character] = sprites[character];
                            }
                            if (spriteEntranceParam.EntranceTransition != SpriteEntranceScriptParameter.SpriteEntranceTransition.NO_TRANSITION)
                            {
                                switch (spriteEntranceParam.EntranceTransition)
                                {
                                    // These ones will do their thing no matter what
                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_CENTER:
                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_CENTER:
                                        sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        break;

                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER:
                                        if (spriteIsNew)
                                        {
                                            sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        }
                                        break;

                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_IN_LEFT:
                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.PEEK_RIGHT_TO_LEFT:
                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_LEFT:
                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_LEFT_FAST:
                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_LEFT_SLOW:
                                        if (spriteIsNew)
                                        {
                                            sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.LEFT.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        }
                                        else if (previousCharacter != character && (spriteEntranceParam.EntranceTransition == SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_RIGHT_TO_LEFT_FAST))
                                        {
                                            sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        }
                                        break;

                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_RIGHT:
                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_RIGHT_FAST:
                                    case SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_RIGHT_SLOW:
                                        if (spriteIsNew)
                                        {
                                            sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.RIGHT.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        }
                                        else if (previousCharacter != character && (spriteEntranceParam.EntranceTransition == SpriteEntranceScriptParameter.SpriteEntranceTransition.SLIDE_LEFT_TO_RIGHT_FAST))
                                        {
                                            sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        }
                                        break;
                                }
                            }
                            else if (sprites.TryGetValue(character, out PositionedSprite sprite))
                            {
                                sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = sprite.Positioning, PalEffect = spritePaint };
                            }

                            if (spriteShakeParam.ShakeEffect != SpriteShakeScriptParameter.SpriteShakeEffect.NONE && sprites.ContainsKey(character))
                            {
                                switch (spriteShakeParam.ShakeEffect)
                                {
                                    case SpriteShakeScriptParameter.SpriteShakeEffect.SHAKE_LEFT:
                                        sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.LEFT.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        break;

                                    case SpriteShakeScriptParameter.SpriteShakeEffect.SHAKE_RIGHT:
                                        sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.RIGHT.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        break;

                                    case SpriteShakeScriptParameter.SpriteShakeEffect.SHAKE_CENTER:
                                    case SpriteShakeScriptParameter.SpriteShakeEffect.BOUNCE_HORIZONTAL_CENTER:
                                    case SpriteShakeScriptParameter.SpriteShakeEffect.BOUNCE_HORIZONTAL_CENTER_WITH_SMALL_SHAKES:
                                        sprites[character] = new() { Sprite = spriteParam.Sprite, Positioning = new() { X = SpritePositioning.SpritePosition.CENTER.GetSpriteX(), Layer = layer }, PalEffect = spritePaint };
                                        break;
                                }
                            }

                            previousCharacter = character;
                        }
                    }
                    else if (command.Verb == CommandVerb.INVEST_START)
                    {
                        sprites.Clear();
                        previousSprites.Clear();
                    }
                    previousCommand = command;
                }

                preview.Sprites = [.. sprites.Values.OrderBy(p => p.Positioning.Layer)];

                // Draw dialogue
                ScriptItemCommand lastDialogueCommand = commands.LastOrDefault(c => c.Verb == CommandVerb.DIALOGUE);
                if (commands.FindLastIndex(c => c.Verb == CommandVerb.TOGGLE_DIALOGUE &&
                    !((BoolScriptParameter)c.Parameters[0]).Value) < commands.IndexOf(lastDialogueCommand))
                {
                    DialogueLine line = ((DialogueScriptParameter)lastDialogueCommand.Parameters[0]).Line;
                    SKPaint dialoguePaint = line.Speaker switch
                    {
                        Speaker.MONOLOGUE => DialogueScriptParameter.Paint01,
                        Speaker.INFO => DialogueScriptParameter.Paint04,
                        _ => DialogueScriptParameter.Paint00,
                    };
                    if (!string.IsNullOrEmpty(line.Text))
                    {
                        preview.LastDialogueCommand = lastDialogueCommand;
                    }
                }
            }

            return preview;
        }

        public static (SKBitmap PreviewImage, string ErrorImage) GeneratePreviewImage(ScriptPreview preview, Project project)
        {
            SKBitmap previewBitmap = new(256, 384);
            SKCanvas canvas = new(previewBitmap);
            canvas.DrawColor(SKColors.Black);

            if (!string.IsNullOrEmpty(preview.ErrorImage))
            {
                return (null, preview.ErrorImage);
            }

            if (preview.EpisodeHeader != 0)
            {
                canvas.DrawBitmap(EpisodeHeaderScriptParameter.GetTexture((EpisodeHeaderScriptParameter.Episode)preview.EpisodeHeader, project).GetTexture(), new SKPoint(0, 0));
            }
            else
            {
                if (preview.Kbg is not null)
                {
                    canvas.DrawBitmap(preview.Kbg.GetBackground(), new SKPoint(0, 0));
                }

                if (preview.Place is not null)
                {
                    canvas.DrawBitmap(preview.Place.GetPreview(project), new SKPoint(5, 40));
                }

                foreach (var chibi in preview.TopScreenChibis)
                {
                    SKBitmap chibiFrame = chibi.Chibi.ChibiAnimations.First().Value.ElementAt(0).Frame;
                    canvas.DrawBitmap(chibiFrame, new SKPoint(chibi.X, chibi.Y));
                }

                if (preview.ChibiEmote.EmotingChibi is not null)
                {
                    SKBitmap emotes = project.Grp.GetFileByName("SYS_ADV_T08DNX").GetImage(width: 32, transparentIndex: 0);
                    int chibiY = preview.TopScreenChibis.First(c => c.Chibi == preview.ChibiEmote.EmotingChibi).Y;
                    canvas.DrawBitmap(emotes, new SKRect(0, preview.ChibiEmote.InternalYOffset, 32, preview.ChibiEmote.InternalYOffset + 32), new SKRect(preview.ChibiEmote.ExternalXOffset + 16, chibiY - 32, preview.ChibiEmote.ExternalXOffset + 48, chibiY));
                }
            }

            // Draw background
            if (preview.Background is not null)
            {
                switch (preview.Background.BackgroundType)
                {
                    case BgType.TEX_CG_DUAL_SCREEN:
                        SKBitmap dualScreenBg = preview.Background.GetBackground();
                        if (preview.BgScrollCommand is not null && ((BgScrollDirectionScriptParameter)preview.BgScrollCommand.Parameters[0]).ScrollDirection == BgScrollDirectionScriptParameter.BgScrollDirection.DOWN)
                        {
                            canvas.DrawBitmap(dualScreenBg, new SKRect(0, preview.Background.Graphic2.Height - 192, 256, preview.Background.Graphic2.Height), new SKRect(0, 0, 256, 192));
                            int bottomScreenX = dualScreenBg.Height - 192;
                            canvas.DrawBitmap(dualScreenBg, new SKRect(0, bottomScreenX, 256, bottomScreenX + 192), new SKRect(0, 192, 256, 384));
                        }
                        else
                        {
                            canvas.DrawBitmap(dualScreenBg, new SKRect(0, 0, 256, 192), new SKRect(0, 0, 256, 192));
                            canvas.DrawBitmap(dualScreenBg, new SKRect(0, preview.Background.Graphic2.Height, 256, preview.Background.Graphic2.Height + 192), new SKRect(0, 192, 256, 384));
                        }
                        break;

                    case BgType.TEX_CG_SINGLE:
                        if (preview.BgPositionBool || (preview.BgScrollCommand is not null && ((BgScrollDirectionScriptParameter)preview.BgScrollCommand.Parameters[0]).ScrollDirection == BgScrollDirectionScriptParameter.BgScrollDirection.DOWN))
                        {
                            SKBitmap bgBitmap = preview.Background.GetBackground();
                            canvas.DrawBitmap(bgBitmap, new SKRect(0, bgBitmap.Height - 192, bgBitmap.Width, bgBitmap.Height),
                                new SKRect(0, 192, 256, 384));
                        }
                        else
                        {
                            canvas.DrawBitmap(preview.Background.GetBackground(), new SKPoint(0, 192));
                        }
                        break;

                    case BgType.TEX_CG_WIDE:
                        if (preview.BgScrollCommand is not null && ((BgScrollDirectionScriptParameter)preview.BgScrollCommand.Parameters[0]).ScrollDirection == BgScrollDirectionScriptParameter.BgScrollDirection.RIGHT)
                        {
                            SKBitmap bgBitmap = preview.Background.GetBackground();
                            canvas.DrawBitmap(bgBitmap, new SKRect(bgBitmap.Width - 256, 0, bgBitmap.Width, 192), new SKRect(0, 192, 256, 384));
                        }
                        else
                        {
                            canvas.DrawBitmap(preview.Background.GetBackground(), new SKPoint(0, 192));
                        }
                        break;

                    case BgType.TEX_CG:
                        canvas.DrawBitmap(preview.Background.GetBackground(), new SKPoint(0, 192));
                        break;

                    default:
                        canvas.DrawBitmap(preview.Background.GetBackground(), new SKPoint(0, 192), PaletteEffectScriptParameter.GetPaletteEffectPaint(preview.BgPalEffect));
                        break;
                }
            }

            if (preview.Item.Item is not null)
            {
                int width = preview.Item.Item.ItemGraphic.Width;
                switch (preview.Item.Location)
                {
                    case ItemItem.ItemLocation.Left:
                        canvas.DrawBitmap(preview.Item.Item.ItemGraphic.GetImage(transparentIndex: 0), 128 - width, 204);
                        break;

                    case ItemItem.ItemLocation.Center:
                        canvas.DrawBitmap(preview.Item.Item.ItemGraphic.GetImage(transparentIndex: 0), 128 - width / 2, 204);
                        break;

                    case ItemItem.ItemLocation.Right:
                        canvas.DrawBitmap(preview.Item.Item.ItemGraphic.GetImage(transparentIndex: 0), 128, 204);
                        break;

                    default:
                    case ItemItem.ItemLocation.Exit:
                        break;
                }
            }

            // Draw character sprites
            foreach (PositionedSprite sprite in preview.Sprites)
            {
                if (sprite.Sprite is not null)
                {
                    SKBitmap spriteBitmap = sprite.Sprite.GetClosedMouthAnimation(project)[0].Frame;
                    canvas.DrawBitmap(spriteBitmap, sprite.Positioning.GetSpritePosition(spriteBitmap), sprite.PalEffect);
                }
            }

            // Draw dialogue
            if (preview.LastDialogueCommand is not null)
            {
                DialogueLine line = ((DialogueScriptParameter)preview.LastDialogueCommand.Parameters[0]).Line;
                SKPaint dialoguePaint = line.Speaker switch
                {
                    Speaker.MONOLOGUE => DialogueScriptParameter.Paint01,
                    Speaker.INFO => DialogueScriptParameter.Paint04,
                    _ => DialogueScriptParameter.Paint00,
                };
                if (!string.IsNullOrEmpty(line.Text))
                {
                    canvas.DrawBitmap(project.DialogueBitmap, new SKRect(0, 24, 32, 36), new SKRect(0, 344, 256, 356));
                    SKColor dialogueBoxColor = project.DialogueBitmap.GetPixel(0, 28);
                    canvas.DrawRect(0, 356, 224, 384, new() { Color = dialogueBoxColor });
                    canvas.DrawBitmap(project.DialogueBitmap, new SKRect(0, 37, 32, 64), new SKRect(224, 356, 256, 384));
                    canvas.DrawBitmap(project.SpeakerBitmap, new SKRect(0, 16 * ((int)line.Speaker - 1), 64, 16 * ((int)line.Speaker)),
                        new SKRect(0, 332, 64, 348));

                    canvas.DrawHaroohieText(line.Text, dialoguePaint, project);
                }
            }

            canvas.Flush();
            return (previewBitmap, null);
        }

        public (SKBitmap PreviewImage, string ErrorImage) GeneratePreviewImage(Dictionary<ScriptSection, List<ScriptItemCommand>> commandTree, ScriptItemCommand currentCommand, Project project, ILogger log)
        {
            return GeneratePreviewImage(GetScriptPreview(commandTree, currentCommand, project, log), project);
        }

        public void UpdateEventTableInfo(EventTable evtTbl)
        {
            EventTableEntry entry = evtTbl.Entries.FirstOrDefault(e => e.EventFileIndex == Event.Index);
            if (entry is not null)
            {
                StartReadFlag = entry.FirstReadFlag;
                SfxGroupIndex = entry.SfxGroupIndex;
            }
            else
            {
                StartReadFlag = -1;
                SfxGroupIndex = -1;
            }
        }

        public void PruneLabelsSection(ILogger log)
        {
            if ((Event.LabelsSection?.Objects?.Count ?? 0) - 1 > Event.ScriptSections.Count)
            {
                try
                {
                    for (int i = 0; i < Event.LabelsSection.Objects.Count; i++)
                    {
                        if (Event.LabelsSection.Objects[i].Id == 0)
                        {
                            continue;
                        }
                        if (!Event.ScriptSections.Select(s => s.Name).Contains(Event.LabelsSection.Objects[i].Name.Replace("/", "")))
                        {
                            Event.LabelsSection.Objects.RemoveAt(i);
                            i--;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogException("Error pruning labels!", ex);
                    log.LogWarning($"Script: {Name}, DisplayName: {DisplayName}");
                }
            }
        }

        public override void Refresh(Project project, ILogger log)
        {
            Graph = new();
            Graph.AddVertexRange(Event.ScriptSections);
            CalculateGraphEdges(GetScriptCommandTree(project, log), log);
        }
    }
}
