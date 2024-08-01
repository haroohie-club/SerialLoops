using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib.Script
{

    public class ScriptTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TemplateSection[] Sections { get; set; }

        public ScriptTemplate()
        {
        }

        public ScriptTemplate(params TemplateSection[] sections)
        {
            Sections = sections;
        }

        public ScriptTemplate(string templateName, string templateDescription, Dictionary<ScriptSection, List<ScriptItemCommand>> commands, Project project)
        {
            Name = templateName;
            Description = templateDescription;
            Sections = [.. commands.Select(s => new TemplateSection(s.Key.Name, s.Value, project))];
        }

        public void Apply(ScriptItem script, Project project, ILogger log)
        {
            foreach (TemplateSection section in Sections)
            {
                int sectionIndex = script.Event.ScriptSections.FindIndex(s => s.Name == section.Name);
                if (sectionIndex < 0)
                {
                    sectionIndex = script.Event.ScriptSections.Count;
                    script.Event.LabelsSection.Objects.Add(new() { Id = (short)(script.Event.LabelsSection.Objects.Count > 0 ? script.Event.LabelsSection.Objects.Max(l => l.Id) + 1 : 1001), Name = section.Name });
                    script.Event.ScriptSections.Add(new() { Name = section.Name, CommandsAvailable = EventFile.CommandsAvailable });
                }
            }
            // We add the sections first and then do it a second time in order to allow ScriptSectionParameters to be able to find their sections
            foreach (TemplateSection section in Sections)
            {
                int sectionIndex = script.Event.ScriptSections.FindIndex(s => s.Name == section.Name);
                for (int i = 0; i < section.Commands.Length; i++)
                {
                    script.Event.ScriptSections[sectionIndex].Objects.Insert(i,
                        new ScriptItemCommand(script.Event.ScriptSections[sectionIndex], script.Event, i, project, section.Commands[i].Verb, [.. section.Commands[i].Parameters.Select(p => p.ParseAndApply(project, script.Event, log))]).Invocation);
                }
            }
        }
    }

    public class TemplateSection
    {
        public string Name { get; set; }
        public TemplateScriptCommand[] Commands { get; set; }

        public TemplateSection()
        {
        }

        public TemplateSection(string name, params TemplateScriptCommand[] commands)
        {
            Name = name;
            Commands = commands;
        }

        public TemplateSection(string name, IEnumerable<ScriptItemCommand> commands, Project project)
        {
            Name = name;
            Commands = [.. commands.Select(c => new TemplateScriptCommand(c, project))];
        }
    }

    public class TemplateScriptCommand
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EventFile.CommandVerb Verb { get; set; }
        public TemplateScriptParameter[] Parameters { get; set; }

        public TemplateScriptCommand()
        {
        }

        public TemplateScriptCommand(EventFile.CommandVerb commandVerb, params TemplateScriptParameter[] parameters)
        {
            Verb = commandVerb;
            Parameters = parameters;
        }

        public TemplateScriptCommand(ScriptItemCommand command, Project project)
        {
            Verb = command.Verb;
            Parameters = [.. command.Parameters.Select(p => new TemplateScriptParameter(p, project))];
        }
    }

    public class TemplateScriptParameter
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ScriptParameter.ParameterType ParameterType { get; set; }
        public string ParameterName { get; set; }
        public string Value { get; set; }

        public TemplateScriptParameter()
        {
        }

        public TemplateScriptParameter(ScriptParameter.ParameterType type, string name, string value)
        {
            ParameterType = type;
            ParameterName = name;
            Value = value;
        }

        public TemplateScriptParameter(ScriptParameter parameter, Project project)
        {
            ParameterType = parameter.Type;
            ParameterName = parameter.Name;
            EncodeValue(parameter, project);
        }

        private void EncodeValue(ScriptParameter parameter, Project project)
        {
            switch (parameter.Type)
            {
                case ScriptParameter.ParameterType.BGM_MODE:
                    Value = $"{(short)((BgmModeScriptParameter)parameter).Mode}";
                    break;

                case ScriptParameter.ParameterType.BGM:
                    Value = ((BgmScriptParameter)parameter).Bgm?.Name ?? "none";
                    break;

                case ScriptParameter.ParameterType.BG:
                    Value = ((BgScriptParameter)parameter).Background?.Name ?? "none";
                    break;

                case ScriptParameter.ParameterType.BG_SCROLL_DIRECTION:
                    Value = $"{(short)((BgScrollDirectionScriptParameter)parameter).ScrollDirection}";
                    break;

                case ScriptParameter.ParameterType.BOOL:
                    BoolScriptParameter boolParam = (BoolScriptParameter)parameter;
                    Value = $"{boolParam.Value},{boolParam.TrueValue},{boolParam.FalseValue}";
                    break;

                case ScriptParameter.ParameterType.CHESS_FILE:
                    Value = ((ChessFileScriptParameter)parameter).ChessFileIndex.ToString();
                    break;

                case ScriptParameter.ParameterType.CHESS_PIECE:
                    Value = ((ChessPieceScriptParameter)parameter).ChessPiece.ToString();
                    break;

                case ScriptParameter.ParameterType.CHESS_SPACE:
                    Value = ((ChessSpaceScriptParameter)parameter).SpaceIndex.ToString();
                    break;

                case ScriptParameter.ParameterType.CHIBI_EMOTE:
                    Value = $"{(short)((ChibiEmoteScriptParameter)parameter).Emote}";
                    break;

                case ScriptParameter.ParameterType.CHIBI_ENTER_EXIT:
                    Value = $"{(short)((ChibiEnterExitScriptParameter)parameter).Mode}";
                    break;

                case ScriptParameter.ParameterType.CHIBI:
                    Value = ((ChibiScriptParameter)parameter).Chibi?.Name ?? "none";
                    break;

                case ScriptParameter.ParameterType.COLOR_MONOCHROME:
                    Value = $"{(short)((ColorMonochromeScriptParameter)parameter).ColorType}";
                    break;

                case ScriptParameter.ParameterType.COLOR:
                    ColorScriptParameter colorParam = (ColorScriptParameter)parameter;
                    Value = $"{colorParam.Color.Red},{colorParam.Color.Green},{colorParam.Color.Blue}";
                    break;

                case ScriptParameter.ParameterType.CONDITIONAL:
                    Value = ((ConditionalScriptParameter)parameter).Conditional;
                    break;

                case ScriptParameter.ParameterType.CHARACTER:
                    Value = $"{(short)((DialoguePropertyScriptParameter)parameter).Character.MessageInfo.Character}";
                    break;

                case ScriptParameter.ParameterType.DIALOGUE:
                    DialogueScriptParameter dialogueParam = (DialogueScriptParameter)parameter;
                    Value = $"{(short)dialogueParam.Line.Speaker}||{dialogueParam.Line.Text.GetSubstitutedString(project)}";
                    break;

                case ScriptParameter.ParameterType.EPISODE_HEADER:
                    Value = $"{(short)((EpisodeHeaderScriptParameter)parameter).EpisodeHeaderIndex}";
                    break;

                case ScriptParameter.ParameterType.FLAG:
                    Value = ((FlagScriptParameter)parameter).Id.ToString();
                    break;

                case ScriptParameter.ParameterType.FRIENDSHIP_LEVEL:
                    Value = $"{(short)((FriendshipLevelScriptParameter)parameter).Character}";
                    break;

                case ScriptParameter.ParameterType.ITEM_LOCATION:
                    Value = $"{(short)((ItemLocationScriptParameter)parameter).Location}";
                    break;

                case ScriptParameter.ParameterType.ITEM:
                    Value = ((ItemScriptParameter)parameter).ItemIndex.ToString();
                    break;

                case ScriptParameter.ParameterType.ITEM_TRANSITION:
                    Value = $"{(short)((ItemTransitionScriptParameter)parameter).Transition}";
                    break;

                case ScriptParameter.ParameterType.MAP:
                    Value = ((MapScriptParameter)parameter).Map?.Name ?? "none";
                    break;

                case ScriptParameter.ParameterType.OPTION:
                    Value = ((OptionScriptParameter)parameter).Option?.Text ?? string.Empty;
                    break;

                case ScriptParameter.ParameterType.PALETTE_EFFECT:
                    Value = $"{(short)((PaletteEffectScriptParameter)parameter).Effect}";
                    break;

                case ScriptParameter.ParameterType.PLACE:
                    Value = ((PlaceScriptParameter)parameter).Place?.Index.ToString() ?? "0";
                    break;

                case ScriptParameter.ParameterType.SCREEN:
                    Value = $"{(short)((ScreenScriptParameter)parameter).Screen}";
                    break;

                case ScriptParameter.ParameterType.SCRIPT_SECTION:
                    Value = ((ScriptSectionScriptParameter)parameter).Section?.Name ?? "none";
                    break;

                case ScriptParameter.ParameterType.SFX_MODE:
                    Value = $"{(short)((SfxModeScriptParameter)parameter).Mode}";
                    break;

                case ScriptParameter.ParameterType.SFX:
                    Value = ((SfxScriptParameter)parameter).Sfx?.Name ?? "none";
                    break;

                case ScriptParameter.ParameterType.SHORT:
                    Value = ((ShortScriptParameter)parameter).Value.ToString();
                    break;

                case ScriptParameter.ParameterType.SPRITE_ENTRANCE:
                    Value = $"{(short)((SpriteEntranceScriptParameter)parameter).EntranceTransition}";
                    break;

                case ScriptParameter.ParameterType.SPRITE_EXIT:
                    Value = $"{(short)((SpriteExitScriptParameter)parameter).ExitTransition}";
                    break;

                case ScriptParameter.ParameterType.SPRITE:
                    Value = ((SpriteScriptParameter)parameter).Sprite?.Index.ToString() ?? "0";
                    break;

                case ScriptParameter.ParameterType.SPRITE_SHAKE:
                    Value = $"{(short)((SpriteShakeScriptParameter)parameter).ShakeEffect}";
                    break;

                case ScriptParameter.ParameterType.TEXT_ENTRANCE_EFFECT:
                    Value = $"{(short)((TextEntranceEffectScriptParameter)parameter).EntranceEffect}";
                    break;

                case ScriptParameter.ParameterType.TOPIC:
                    Value = ((TopicScriptParameter)parameter).TopicId.ToString();
                    break;

                case ScriptParameter.ParameterType.TRANSITION:
                    Value = $"{(short)((TransitionScriptParameter)parameter).Transition}";
                    break;

                case ScriptParameter.ParameterType.VOICE_LINE:
                    Value = ((VoicedLineScriptParameter)parameter).VoiceLine?.Name ?? "none";
                    break;

                default:
                    Value = string.Empty;
                    break;
            }
        }

        public ScriptParameter ParseAndApply(Project project, EventFile evt, ILogger log)
        {
            try
            {
                string value = Value;
                string localizedName = project.Localize(ParameterName);
                switch (ParameterType)
                {
                    case ScriptParameter.ParameterType.BGM_MODE:
                        return new BgmModeScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.BGM:
                        return new BgmScriptParameter(localizedName, (BackgroundMusicItem)project.Items.FirstOrDefault(i => i.Name == value));

                    case ScriptParameter.ParameterType.BG:
                        return new BgScriptParameter(localizedName, (BackgroundItem)project.Items.FirstOrDefault(i => i.Name == value), value.Contains("KBG"));

                    case ScriptParameter.ParameterType.BG_SCROLL_DIRECTION:
                        return new BgScrollDirectionScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.BOOL:
                        string[] boolValues = value.Split(',');
                        return new BoolScriptParameter(localizedName, bool.Parse(boolValues[0]), short.Parse(boolValues[1]), short.Parse(boolValues[2]));

                    case ScriptParameter.ParameterType.CHESS_FILE:
                        return new ChessFileScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.CHESS_PIECE:
                        return new ChessPieceScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.CHESS_SPACE:
                        return new ChessSpaceScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.CHIBI_EMOTE:
                        return new ChibiEmoteScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.CHIBI_ENTER_EXIT:
                        return new ChibiEnterExitScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.CHIBI:
                        return new ChibiScriptParameter(localizedName, (ChibiItem)project.Items.FirstOrDefault(i => i.Name == value));

                    case ScriptParameter.ParameterType.COLOR_MONOCHROME:
                        return new ColorMonochromeScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.COLOR:
                        string[] colorValues = value.Split(',');
                        ColorScriptParameter colorParam = new(localizedName, short.Parse(colorValues[0]));
                        colorParam.SetGreen(short.Parse(colorValues[1]));
                        colorParam.SetBlue(short.Parse(colorValues[2]));
                        return colorParam;

                    case ScriptParameter.ParameterType.CONDITIONAL:
                        return new ConditionalScriptParameter(localizedName, value);

                    case ScriptParameter.ParameterType.CHARACTER:
                        return new DialoguePropertyScriptParameter(localizedName, (CharacterItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Character && (short)((CharacterItem)i).MessageInfo.Character == short.Parse(value)));

                    case ScriptParameter.ParameterType.DIALOGUE:
                        string[] dialougeValues = value.Split("||");
                        DialogueLine line = new(dialougeValues[1].GetOriginalString(project), evt)
                        {
                            Speaker = ((CharacterItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Character && (short)((CharacterItem)i).MessageInfo.Character == short.Parse(dialougeValues[0]))).MessageInfo.Character
                        };
                        evt.DialogueSection.Objects.Add(line);
                        evt.DialogueLines.Add(line);
                        return new DialogueScriptParameter(localizedName, line);

                    case ScriptParameter.ParameterType.EPISODE_HEADER:
                        return new EpisodeHeaderScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.FLAG:
                        return new FlagScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.FRIENDSHIP_LEVEL:
                        return new FriendshipLevelScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.ITEM_LOCATION:
                        return new ItemLocationScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.ITEM:
                        return new ItemScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.ITEM_TRANSITION:
                        return new ItemTransitionScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.MAP:
                        return new MapScriptParameter(localizedName, (MapItem)project.Items.FirstOrDefault(i => i.Name == value));

                    case ScriptParameter.ParameterType.OPTION:
                        if (evt.ChoicesSection.Objects.Any(c => c.Text == value))
                        {
                            return new OptionScriptParameter(localizedName, evt.ChoicesSection.Objects.First(c => c.Text == value));
                        }
                        else
                        {
                            ChoicesSectionEntry choice = new() { Id = evt.ChoicesSection.Objects.Count > 0 ? evt.ChoicesSection.Objects.Max(c => c.Id) + 1 : 1001 };
                            evt.ChoicesSection.Objects.Add(choice);
                            return new OptionScriptParameter(localizedName, choice);
                        }

                    case ScriptParameter.ParameterType.PALETTE_EFFECT:
                        return new PaletteEffectScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.PLACE:
                        return new PlaceScriptParameter(localizedName, (PlaceItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Place && ((PlaceItem)i).Index == short.Parse(value)));

                    case ScriptParameter.ParameterType.SCREEN:
                        return new ScreenScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.SCRIPT_SECTION:
                        return new ScriptSectionScriptParameter(localizedName, evt.ScriptSections.FirstOrDefault(s => s.Name == value));

                    case ScriptParameter.ParameterType.SFX_MODE:
                        return new SfxModeScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.SFX:
                        return new SfxScriptParameter(localizedName, (SfxItem)project.Items.FirstOrDefault(s => s.Name == value));

                    case ScriptParameter.ParameterType.SHORT:
                        return new ShortScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.SPRITE_ENTRANCE:
                        return new SpriteEntranceScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.SPRITE_EXIT:
                        return new SpriteExitScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.SPRITE:
                        return new SpriteScriptParameter(localizedName, (CharacterSpriteItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && ((CharacterSpriteItem)i).Index == short.Parse(value)));

                    case ScriptParameter.ParameterType.SPRITE_SHAKE:
                        return new SpriteShakeScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.TEXT_ENTRANCE_EFFECT:
                        return new TextEntranceEffectScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.TOPIC:
                        return new TopicScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.TRANSITION:
                        return new TransitionScriptParameter(localizedName, short.Parse(value));

                    case ScriptParameter.ParameterType.VOICE_LINE:
                        return new VoicedLineScriptParameter(localizedName, (VoicedLineItem)project.Items.FirstOrDefault(i => i.Name == value));

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                log.LogException(string.Format(project.Localize("Failed to parse script parameter {0} of type {1} with parameter '{2}'!"), ParameterName, ParameterType, Value), ex);
                return null;
            }
        }
    }
}
