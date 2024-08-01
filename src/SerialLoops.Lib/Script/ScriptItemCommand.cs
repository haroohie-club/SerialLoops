using System;
using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using QuikGraph;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.Search;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.Lib.Script
{
    public class ScriptItemCommand
    {
        public ScriptCommandInvocation Invocation { get; set; }
        public CommandVerb Verb { get; set; }
        public List<ScriptParameter> Parameters { get; set; }
        public ScriptSection Section { get; set; }
        public EventFile Script { get; set; }
        public int Index { get; set; }
        public Project Project { get; set; }

        public static ScriptItemCommand FromInvocation(ScriptCommandInvocation invocation, ScriptSection section, int index, EventFile eventFile, Project project, Func<string, string> localize, ILogger log)
        {
            return new()
            {
                Invocation = invocation,
                Verb = (CommandVerb)Enum.Parse(typeof(CommandVerb), invocation.Command.Mnemonic),
                Parameters = GetScriptParameters(invocation, section, eventFile, project, localize, log),
                Section = section,
                Index = index,
                Script = eventFile,
                Project = project,
            };
        }

        public ScriptItemCommand()
        {
        }
        public ScriptItemCommand(ScriptSection section, EventFile script, int index, Project project, CommandVerb verb, params ScriptParameter[] parameters)
        {
            Section = section;
            Script = script;
            Index = index;
            Project = project;
            Verb = verb;
            Parameters = [.. parameters];

            List<short> shortParams = parameters.SelectMany(p =>
            {
                return p.Type switch
                {
                    ScriptParameter.ParameterType.CHARACTER => p.GetValues(project.MessInfo),
                    ScriptParameter.ParameterType.CONDITIONAL or ScriptParameter.ParameterType.DIALOGUE or ScriptParameter.ParameterType.OPTION or ScriptParameter.ParameterType.SCRIPT_SECTION => p.GetValues(script),
                    _ => p.GetValues(),
                };
            }).ToList();
            shortParams.AddRange(new short[16 - shortParams.Count]);
            Invocation = new(CommandsAvailable.First(c => c.Mnemonic == verb.ToString())) { Parameters = shortParams };
        }
        public ScriptItemCommand(CommandVerb verb, params ScriptParameter[] parameters)
        {
            Verb = verb;
            Parameters = [.. parameters];
        }

        public List<ScriptItemCommand> WalkCommandGraph(Dictionary<ScriptSection, List<ScriptItemCommand>> commandTree, AdjacencyGraph<ScriptSection, ScriptSectionEdge> graph)
        {
            List<ScriptItemCommand> commands = [];

            Func<ScriptSectionEdge, double> weightFunction = new((ScriptSectionEdge edge) =>
            {
                return 1;
            });

            if (Section != commandTree.Keys.First())
            {
                DepthFirstSearchAlgorithm<ScriptSection, ScriptSectionEdge> dfs = new(graph);
                var observer = new VertexPredecessorRecorderObserver<ScriptSection, ScriptSectionEdge>();
                using (observer.Attach(dfs))
                {
                    dfs.Compute(commandTree.Keys.First());
                }
                bool success = observer.TryGetPath(Section, out IEnumerable<ScriptSectionEdge> path);

                if (!success)
                {
                    return null;
                }

                foreach (ScriptSectionEdge edge in path)
                {
                    commands.AddRange(commandTree[edge.Source]);
                }
            }
            commands.AddRange(commandTree[Section].TakeWhile(c => c.Index != Index));
            commands.Add(this);

            return commands;
        }

        private static List<ScriptParameter> GetScriptParameters(ScriptCommandInvocation invocation, ScriptSection section, EventFile eventFile, Project project, Func<string, string> localize, ILogger log)
        {
            List<ScriptParameter> parameters = [];

            for (int i = 0; i < invocation.Parameters.Count; i++)
            {
                short parameter = invocation.Parameters[i];
                switch ((CommandVerb)Enum.Parse(typeof(CommandVerb), invocation.Command.Mnemonic))
                {
                    case CommandVerb.DIALOGUE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new DialogueScriptParameter(localize("Dialogue"), GetDialogueLine(parameter, eventFile)));
                                break;
                            case 1:
                                parameters.Add(new SpriteScriptParameter(localize("Sprite"), (CharacterSpriteItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character_Sprite && parameter == ((CharacterSpriteItem)i).Index)));
                                break;
                            case 2:
                                parameters.Add(new SpriteEntranceScriptParameter(localize("Sprite Entrance Transition"), parameter));
                                break;
                            case 3:
                                parameters.Add(new SpriteExitScriptParameter(localize("Sprite Exit/Move Transition"), parameter));
                                break;
                            case 4:
                                parameters.Add(new SpriteShakeScriptParameter(localize("Sprite Shake"), parameter));
                                break;
                            case 5:
                                parameters.Add(new VoicedLineScriptParameter(localize("Voice Line"), (VoicedLineItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Voice && parameter == ((VoicedLineItem)i).Index)));
                                break;
                            case 6:
                                parameters.Add(new DialoguePropertyScriptParameter(localize("Text Voice Font"), (CharacterItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character && ((CharacterItem)i).MessageInfo.Character == project.MessInfo.MessageInfos[parameter].Character)));
                                break;
                            case 7:
                                parameters.Add(new DialoguePropertyScriptParameter(localize("Text Speed"), (CharacterItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Character && ((CharacterItem)i).MessageInfo.Character == project.MessInfo.MessageInfos[parameter].Character)));
                                break;
                            case 8:
                                parameters.Add(new TextEntranceEffectScriptParameter(localize("Text Entrance Effect"), parameter));
                                break;
                            case 9:
                                parameters.Add(new ShortScriptParameter(localize("Sprite Layer"), parameter));
                                break;
                            case 10:
                                parameters.Add(new BoolScriptParameter(localize("Don't Clear Text"), parameter == 1));
                                break;
                            case 11:
                                parameters.Add(new BoolScriptParameter(localize("Disable Lip Flap"), parameter == 1));
                                break;

                        }
                        break;
                    case CommandVerb.KBG_DISP:
                        if (i == 0)
                        {
                            parameters.Add(new BgScriptParameter(localize("\"Kinetic\" Background"), (BackgroundItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter), kinetic: true));
                        }
                        break;
                    case CommandVerb.PIN_MNL:
                        if (i == 0)
                        {
                            parameters.Add(new DialogueScriptParameter(localize("Dialogue"), GetDialogueLine(parameter, eventFile)));
                        }
                        break;
                    case CommandVerb.BG_DISP:
                    case CommandVerb.BG_DISP2:
                        if (i == 0)
                        {
                            ItemDescription bgItem = project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter)
                                ?? project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_BG);
                            parameters.Add(new BgScriptParameter(localize("Background"), (BackgroundItem)bgItem, kinetic: false));
                        }
                        break;
                    case CommandVerb.SCREEN_FADEIN:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter(localize("Fade Time (Frames)"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter(localize("Fade In Percentage"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ScreenScriptParameter(localize("Location"), parameter));
                                break;
                            case 3:
                                parameters.Add(new ColorMonochromeScriptParameter(localize("Color"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SCREEN_FADEOUT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter(localize("Fade Time (Frames)"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter(localize("Fade Out Percentage"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ColorScriptParameter(localize("Custom Color"), parameter));
                                break;
                            case 3:
                                ((ColorScriptParameter)parameters.Last()).SetGreen(parameter);
                                break;
                            case 4:
                                ((ColorScriptParameter)parameters.Last()).SetBlue(parameter);
                                break;
                            case 5:
                                parameters.Add(new ScreenScriptParameter(localize("Location"), parameter));
                                break;
                            case 6:
                                parameters.Add(new ColorMonochromeScriptParameter(localize("Color"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SCREEN_FLASH:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter(localize("Fade In Time (Frames)"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter(localize("Hold Time (Frames)"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter(localize("Fade Out Time (Frames)"), parameter));
                                break;
                            case 3:
                                parameters.Add(new ColorScriptParameter(localize("Color"), parameter));
                                break;
                            case 4:
                                ((ColorScriptParameter)parameters.Last()).SetGreen(parameter);
                                break;
                            case 5:
                                ((ColorScriptParameter)parameters.Last()).SetBlue(parameter);
                                break;
                        }
                        break;
                    case CommandVerb.SND_PLAY:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new SfxScriptParameter(localize("Sound"), (SfxItem)project.Items.First(s => s.Type == ItemDescription.ItemType.SFX && ((SfxItem)s).Index == parameter)));
                                break;
                            case 1:
                                parameters.Add(new SfxModeScriptParameter(localize("Mode"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter(localize("Volume"), parameter));
                                break;
                            case 3:
                                parameters.Add(new BoolScriptParameter(localize("Load Sound"), parameter == -1, trueValue: -1));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter(localize("Crossfade Time (Frames)"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.BGM_PLAY:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgmScriptParameter(localize("Music"), (BackgroundMusicItem)project.Items.First(i => i.Type == ItemDescription.ItemType.BGM && ((BackgroundMusicItem)i).Index == parameter)));
                                break;
                            case 1:
                                parameters.Add(new BgmModeScriptParameter(localize("Mode"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter(localize("Volume"), parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter(localize("Fade In Time (Frames)"), parameter));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter(localize("Fade Out Time (Frames)"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.VCE_PLAY:
                        if (i == 0)
                        {
                            parameters.Add(new VoicedLineScriptParameter(localize("Voice Line"), (VoicedLineItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Voice && parameter == ((VoicedLineItem)i).Index)));
                        }
                        break;
                    case CommandVerb.FLAG:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new FlagScriptParameter(localize("Flag"), parameter));
                                break;
                            case 1:
                                parameters.Add(new BoolScriptParameter(localize("Set/Clear"), parameter == 1));
                                break;
                        }
                        break;
                    case CommandVerb.TOPIC_GET:
                        if (i == 0)
                        {
                            parameters.Add(new TopicScriptParameter(localize("Topic"), parameter));
                        }
                        break;
                    case CommandVerb.TOGGLE_DIALOGUE:
                        if (i == 0)
                        {
                            parameters.Add(new BoolScriptParameter(localize("Show"), parameter == 1));
                        }
                        break;
                    case CommandVerb.SELECT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new OptionScriptParameter(localize("Option 1"), eventFile.ChoicesSection.Objects[parameter]));
                                break;
                            case 1:
                                parameters.Add(new OptionScriptParameter(localize("Option 2"), eventFile.ChoicesSection.Objects[parameter]));
                                break;
                            case 2:
                                parameters.Add(new OptionScriptParameter(localize("Option 3"), eventFile.ChoicesSection.Objects[parameter]));
                                break;
                            case 3:
                                parameters.Add(new OptionScriptParameter(localize("Option 4"), eventFile.ChoicesSection.Objects[parameter]));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter(localize("Display Flag 1"), parameter));
                                break;
                            case 5:
                                parameters.Add(new ShortScriptParameter(localize("Display Flag 2"), parameter));
                                break;
                            case 6:
                                parameters.Add(new ShortScriptParameter(localize("Display Flag 3"), parameter));
                                break;
                            case 7:
                                parameters.Add(new ShortScriptParameter(localize("Display Flag 4"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SCREEN_SHAKE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter(localize("Duration (Frames)"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter(localize("Horizontal Intensity"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter(localize("Vertical Intensity"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.GOTO:
                        if (i == 0)
                        {
                            if (parameter == 0)
                            {
                                parameter = eventFile.LabelsSection.Objects.FirstOrDefault(l => l.Id != 00)?.Id ?? 0;
                                if (parameter == 0)
                                {
                                    log.LogError($"Adding GOTO command in section {section.Name} failed: no section with a label exists");
                                }
                            }
                            try
                            {
                                parameters.Add(new ScriptSectionScriptParameter(localize("Script Section"), eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                            }
                            catch (InvalidOperationException)
                            {
                                log.LogWarning($"Failed to evaluate script section for GOTO command in section {section.Name}: references a non-existent section. Resetting!");
                                parameter = eventFile.LabelsSection.Objects.FirstOrDefault(l => l.Id != 00)?.Id ?? 0;
                                if (parameter == 0)
                                {
                                    log.LogError($"Adding GOTO command in section {section.Name} failed: no section with a label exists!");
                                }
                                else
                                {
                                    parameters.Add(new ScriptSectionScriptParameter(localize("Script Section"), eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                }
                            }
                        }
                        break;
                    case CommandVerb.SCENE_GOTO:
                    case CommandVerb.SCENE_GOTO2:
                        if (i == 0)
                        {
                            parameters.Add(new ConditionalScriptParameter(localize("Scene"), eventFile.ConditionalsSection.Objects[parameter]));
                        }
                        break;
                    case CommandVerb.WAIT:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter(localize("Wait Time (Frames)"), parameter));
                        }
                        break;
                    case CommandVerb.VGOTO:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ConditionalScriptParameter(localize("Conditional"), eventFile.ConditionalsSection.Objects[parameter]));
                                break;
                            // 1 is unused
                            case 2:
                                if (parameter == 0)
                                {
                                    parameter = eventFile.LabelsSection.Objects.FirstOrDefault(l => l.Id != 00)?.Id ?? 0;
                                    if (parameter == 0)
                                    {
                                        log.LogError($"Adding VGOTO command in section {section.Name} failed: no section with a label exists");
                                    }
                                }
                                try
                                {
                                    parameters.Add(new ScriptSectionScriptParameter(localize("Script Section"), eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                }
                                catch (InvalidOperationException)
                                {
                                    log.LogWarning($"Failed to evaluate script section for VGOTO command in section {section.Name}: references a non-existent section. Resetting!");
                                    parameter = eventFile.LabelsSection.Objects.FirstOrDefault(l => l.Id != 00)?.Id ?? 0;
                                    if (parameter == 0)
                                    {
                                        log.LogError($"Adding GOTO command in section {section.Name} failed: no section with a label exists!");
                                    }
                                    else
                                    {
                                        parameters.Add(new ScriptSectionScriptParameter(localize("Script Section"), eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                    }
                                }
                                break;
                        }
                        break;
                    case CommandVerb.HARUHI_METER:
                        switch (i)
                        {
                            // 0 is unused
                            case 1:
                                parameters.Add(new ShortScriptParameter(localize("Add"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter(localize("Set"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.HARUHI_METER_NOSHOW:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter(localize("Add"), parameter));
                        }
                        break;
                    case CommandVerb.PALEFFECT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new PaletteEffectScriptParameter(localize("Mode"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter(localize("Time (Frames)"), parameter));
                                break;
                            case 2:
                                parameters.Add(new BoolScriptParameter(localize("Unknown"), parameter > 0));
                                break;
                        }
                        break;
                    case CommandVerb.BG_FADE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgScriptParameter(localize("Background"), (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter), kinetic: false));
                                break;
                            case 1:
                                parameters.Add(new BgScriptParameter(localize("Background (CG)"), (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter), kinetic: false));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter(localize("Fade Time (Frames)"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.TRANS_OUT:
                    case CommandVerb.TRANS_IN:
                        if (i == 0)
                        {
                            parameters.Add(new TransitionScriptParameter(localize("Transition"), parameter));
                        }
                        break;
                    case CommandVerb.SET_PLACE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BoolScriptParameter(localize("Display?"), parameter == 1));
                                break;
                            case 1:
                                parameters.Add(new PlaceScriptParameter(localize("Place"), (PlaceItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Place && ((PlaceItem)i).Index == parameter)));
                                break;
                        }
                        break;
                    case CommandVerb.ITEM_DISPIMG:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ItemScriptParameter(localize("Item"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ItemLocationScriptParameter(localize("Location"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ItemTransitionScriptParameter(localize("Transition"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.LOAD_ISOMAP:
                        if (i == 0)
                        {
                            parameters.Add(new MapScriptParameter(localize("Map"), (MapItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Map && (parameter) == ((MapItem)i).Map.Index)));
                        }
                        break;
                    case CommandVerb.INVEST_START:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter(localize($"Map Character Set"), parameter));
                                break;
                            case 1:
                            case 2:
                            case 3:
                                parameters.Add(new ShortScriptParameter(localize($"unknown0{i}"), parameter));
                                break;
                            case 4:
                                parameters.Add(new ScriptSectionScriptParameter(localize("End Script Section"), eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                        }
                        break;
                    case CommandVerb.CHIBI_EMOTE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChibiScriptParameter(localize("Chibi"), (ChibiItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi && (parameter) == ((ChibiItem)i).TopScreenIndex)));
                                break;
                            case 1:
                                parameters.Add(new ChibiEmoteScriptParameter(localize("Emote"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SKIP_SCENE:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter(localize("Scenes to Skip"), parameter));
                        }
                        break;
                    case CommandVerb.MODIFY_FRIENDSHIP:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new FriendshipLevelScriptParameter(localize("Character"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter(localize("Modify by"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHIBI_ENTEREXIT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChibiScriptParameter(localize("Chibi"), (ChibiItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi && (parameter) == ((ChibiItem)i).TopScreenIndex)));
                                break;
                            case 1:
                                parameters.Add(new ChibiEnterExitScriptParameter(localize("Enter/Exit"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter(localize("Delay (Frames)"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.GLOBAL2D:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter(localize("Value"), parameter));
                        }
                        break;
                    case CommandVerb.CHESS_LOAD:
                        if (i == 0)

                        {
                            parameters.Add(new ChessFileScriptParameter(localize("Chess File"), parameter));
                        }
                        break;
                    case CommandVerb.CHESS_VGOTO:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ScriptSectionScriptParameter(localize("Clear Block"), eventFile.ScriptSections.FirstOrDefault(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                            case 1:
                                parameters.Add(new ScriptSectionScriptParameter(localize("Miss Block"), eventFile.ScriptSections.FirstOrDefault(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                            case 2:
                                parameters.Add(new ScriptSectionScriptParameter(localize("Miss 2 Block"), eventFile.ScriptSections.FirstOrDefault(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_MOVE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChessSpaceScriptParameter(localize("White Space Begin"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ChessSpaceScriptParameter(localize("White Space End"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ChessSpaceScriptParameter(localize("Black Space Begin"), parameter));
                                break;
                            case 3:
                                parameters.Add(new ChessSpaceScriptParameter(localize("Black Space End"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_GUIDE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChessPieceScriptParameter(localize("Piece 1"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ChessPieceScriptParameter(localize("Piece 2"), parameter));
                                break;
                            case 2:
                                parameters.Add(new ChessPieceScriptParameter(localize("Piece 3"), parameter));
                                break;
                            case 3:
                                parameters.Add(new ChessPieceScriptParameter(localize("Piece 4"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_HIGHLIGHT:
                        if (parameter != -1)
                        {
                            parameters.Add(new ChessSpaceScriptParameter(string.Format(localize("Highlight Space {0}"), i), parameter));
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_CROSS:
                        if (parameter != -1)
                        {
                            parameters.Add(new ChessSpaceScriptParameter(string.Format(localize("Cross Space {0}"), i), parameter));
                        }
                        break;
                    case CommandVerb.EPHEADER:
                        if (i == 0)
                        {
                            parameters.Add(new EpisodeHeaderScriptParameter(localize("Episode Header"), parameter));
                        }
                        break;
                    case CommandVerb.CONFETTI:
                        if (i == 0)

                        {
                            parameters.Add(new BoolScriptParameter(localize("Visible?"), parameter == 1));
                        }
                        break;
                    case CommandVerb.BG_DISPCG:
                        switch (i)
                        {
                            case 0:
                                ItemDescription cgItem = project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter)
                                    ?? project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_CG);

                                parameters.Add(new BgScriptParameter(localize("Background"), (BackgroundItem)cgItem, kinetic: false));
                                break;
                            case 1:
                                parameters.Add(new BoolScriptParameter(localize("Display from Bottom"), parameter == 1));
                                break;
                        }
                        break;
                    case CommandVerb.BG_SCROLL:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgScrollDirectionScriptParameter(localize("Scroll Direction"), parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter(localize("Scroll Speed"), parameter));
                                break;
                        }
                        break;
                    case CommandVerb.WAIT_CANCEL:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter(localize("Wait Time (Frames)"), parameter));
                        }
                        break;
                }
            }

            return parameters;
        }

        public override string ToString()
        {
            string str = $"{Verb}";
            if (Verb == CommandVerb.DIALOGUE)
            {
                str += $" {((DialogueScriptParameter)Parameters[0]).Line.Text.GetSubstitutedString(Project)[0..Math.Min(((DialogueScriptParameter)Parameters[0]).Line.Text.Length, 10)]}...";
            }
            else if (Verb == CommandVerb.GOTO)
            {
                str += $" {((ScriptSectionScriptParameter)Parameters[0]).Section.Name}";
            }
            else if (Verb == CommandVerb.VGOTO)
            {
                str += $" {((ConditionalScriptParameter)Parameters[0]).Conditional}, {((ScriptSectionScriptParameter)Parameters[1]).Section.Name}";
            }
            return str;
        }

        private static DialogueLine GetDialogueLine(short index, EventFile eventFile)
        {
            return eventFile.DialogueSection.Objects[index];
        }

        public ScriptItemCommand Clone()
        {
            return new()
            {
                Invocation = Invocation,
                Verb = Verb,
                Parameters = Parameters.Select(p => p.Clone(Project, Script)).ToList(),
                Section = Section,
                Index = Index,
                Script = Script,
                Project = Project,
            };
        }

    }
}
