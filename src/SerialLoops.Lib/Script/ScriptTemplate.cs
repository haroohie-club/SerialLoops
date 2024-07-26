using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using System;
using System.Linq;
using System.Text.Json.Serialization;

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
    }

    public class TemplateScriptParameter
    {
        public string TypeName { get; set; }
        public Type ParameterType { get => Type.GetType(TypeName); set => TypeName = value.FullName; }
        public string ParameterName { get; set; }
        public string Value { get; set; }

        public TemplateScriptParameter()
        {
        }

        public TemplateScriptParameter(Type parameterType, string name, string value)
        {
            ParameterType = parameterType;
            ParameterName = name;
            Value = value;
        }

        public ScriptParameter ParseAndApply(Project project, EventFile evt, ILogger log)
        {
            try
            {
                string value = Value;
                string localizedName = project.Localize(ParameterName);
                if (ParameterType == typeof(BgmModeScriptParameter))
                {
                    return new BgmModeScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(BgmScriptParameter))
                {
                    return new BgmScriptParameter(localizedName, (BackgroundMusicItem)project.Items.FirstOrDefault(i => i.Name == value));
                }
                else if (ParameterType == typeof(BgScriptParameter))
                {
                    return new BgScriptParameter(localizedName, (BackgroundItem)project.Items.FirstOrDefault(i => i.Name == value), value.Contains("KBG"));
                }
                else if (ParameterType == typeof(BgScrollDirectionScriptParameter))
                {
                    return new BgScrollDirectionScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(BoolScriptParameter))
                {
                    string[] values = value.Split(',');
                    return new BoolScriptParameter(localizedName, bool.Parse(values[0]), short.Parse(values[1]), short.Parse(values[2]));
                }
                else if (ParameterType == typeof(ChessFileScriptParameter))
                {
                    return new ChessFileScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ChessPieceScriptParameter))
                {
                    return new ChessPieceScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ChessSpaceScriptParameter))
                {
                    return new ChessSpaceScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ChibiEmoteScriptParameter))
                {
                    return new ChibiEmoteScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ChibiEnterExitScriptParameter))
                {
                    return new ChibiEnterExitScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ChibiScriptParameter))
                {
                    return new ChibiScriptParameter(localizedName, (ChibiItem)project.Items.FirstOrDefault(i => i.Name == value));
                }
                else if (ParameterType == typeof(ColorMonochromeScriptParameter))
                {
                    return new ColorMonochromeScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ColorScriptParameter))
                {
                    string[] values = value.Split(',');
                    ColorScriptParameter param = new(localizedName, short.Parse(values[0]));
                    param.SetGreen(short.Parse(values[1]));
                    param.SetBlue(short.Parse(values[2]));
                    return param;
                }
                else if (ParameterType == typeof(ConditionalScriptParameter))
                {
                    return new ConditionalScriptParameter(localizedName, value);
                }
                else if (ParameterType == typeof(DialoguePropertyScriptParameter))
                {
                    return new DialoguePropertyScriptParameter(localizedName, (CharacterItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Character && (short)((CharacterItem)i).MessageInfo.Character == short.Parse(value)));
                }
                else if (ParameterType == typeof(DialogueScriptParameter))
                {
                    string[] values = value.Split("||");
                    DialogueLine line = new(values[1].GetOriginalString(project), evt)
                    {
                        Speaker = ((CharacterItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Character && (short)((CharacterItem)i).MessageInfo.Character == short.Parse(values[0]))).MessageInfo.Character
                    };
                    evt.DialogueSection.Objects.Add(line);
                    evt.DialogueLines.Add(line);
                    return new DialogueScriptParameter(localizedName, line);
                }
                else if (ParameterType == typeof(EpisodeHeaderScriptParameter))
                {
                    return new EpisodeHeaderScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(FlagScriptParameter))
                {
                    return new FlagScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(FriendshipLevelScriptParameter))
                {
                    return new FriendshipLevelScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ItemLocationScriptParameter))
                {
                    return new ItemLocationScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ItemScriptParameter))
                {
                    return new ItemScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ItemTransitionScriptParameter))
                {
                    return new ItemTransitionScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(MapScriptParameter))
                {
                    return new MapScriptParameter(localizedName, (MapItem)project.Items.FirstOrDefault(i => i.Name == value));
                }
                else if (ParameterType == typeof(OptionScriptParameter))
                {
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
                }
                else if (ParameterType == typeof(PaletteEffectScriptParameter))
                {
                    return new PaletteEffectScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(PlaceScriptParameter))
                {
                    return new PlaceScriptParameter(localizedName, (PlaceItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Place && ((PlaceItem)i).Index == short.Parse(value)));
                }
                else if (ParameterType == typeof(ScreenScriptParameter))
                {
                    return new ScreenScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(ScriptSectionScriptParameter))
                {
                    return new ScriptSectionScriptParameter(localizedName, evt.ScriptSections.FirstOrDefault(s => s.Name == value));
                }
                else if (ParameterType == typeof(SfxModeScriptParameter))
                {
                    return new SfxModeScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(SfxScriptParameter))
                {
                    return new SfxScriptParameter(localizedName, (SfxItem)project.Items.FirstOrDefault(s => s.Name == value));
                }
                else if (ParameterType == typeof(ShortScriptParameter))
                {
                    return new ShortScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(SpriteEntranceScriptParameter))
                {
                    return new SpriteEntranceScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(SpriteExitScriptParameter))
                {
                    return new SpriteExitScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(SpriteScriptParameter))
                {
                    return new SpriteScriptParameter(localizedName, (CharacterSpriteItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && ((CharacterSpriteItem)i).Index == short.Parse(value)));
                }
                else if (ParameterType == typeof(SpriteShakeScriptParameter))
                {
                    return new SpriteShakeScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(TextEntranceEffectScriptParameter))
                {
                    return new TextEntranceEffectScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(TopicScriptParameter))
                {
                    return new TopicScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(TransitionScriptParameter))
                {
                    return new TransitionScriptParameter(localizedName, short.Parse(value));
                }
                else if (ParameterType == typeof(VoicedLineScriptParameter))
                {
                    return new VoicedLineScriptParameter(localizedName, (VoicedLineItem)project.Items.FirstOrDefault(i => i.Name == value));
                }
                else
                {
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
