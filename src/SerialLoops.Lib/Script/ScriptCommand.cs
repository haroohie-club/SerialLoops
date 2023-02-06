using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.Lib.Script
{
    public class ScriptItemCommand
    {
        public CommandVerb Verb { get; set; }
        public List<ScriptParameter> Parameters { get; set; }
        public string Section { get; set; }
        public int Index { get; set; }

        public static ScriptItemCommand FromInvocation(ScriptCommandInvocation invocation, string section, int index, EventFile eventFile, Project project)
        {
            return new()
            {
                Verb = (CommandVerb)Enum.Parse(typeof(CommandVerb), invocation.Command.Mnemonic),
                Parameters = GetScriptParameters(invocation, eventFile, project),
                Section = section == "NONEMiss2" ? "Miss 2 Block" : section,
                Index = index,
            };
        }

        public List<ScriptItemCommand> WalkCommandTree(Dictionary<ScriptSection, List<ScriptItemCommand>> commandTree, LabelsSection labels)
        {
            List<ScriptItemCommand> commands = new();

            int curCommandIndex = 0;
            int curSectionIndex = 0;
            do
            {
                ScriptSection section = commandTree.Keys.ElementAt(curSectionIndex);
                for (curCommandIndex = 0; curCommandIndex < commandTree[section].Count; curCommandIndex++)
                {
                    ScriptItemCommand currentCommand = commandTree[section][curCommandIndex];
                    commands.Add(currentCommand);
                    if (currentCommand.Section == Section && curCommandIndex == Index)
                    {
                        break;
                    }
                    if (currentCommand.Verb == CommandVerb.GOTO)
                    {
                        // -1 bc section is about to be incremented after we break
                        curSectionIndex = commandTree.Keys.ToList()
                            .IndexOf(((ScriptSectionScriptParameter)commandTree[section][curCommandIndex].Parameters[0]).Section) - 1;
                        break;
                    }
                    else if (currentCommand.Verb == CommandVerb.VGOTO &&
                        ((ScriptSectionScriptParameter)currentCommand.Parameters[1]).Section.Name == Section)
                    {
                        // -1 bc section is about to be incremented after we break
                        curSectionIndex = commandTree.Keys.ToList()
                            .IndexOf(((ScriptSectionScriptParameter)commandTree[section][curCommandIndex].Parameters[1]).Section) - 1;
                        break;
                    }
                    else if (currentCommand.Verb == CommandVerb.CHESS_VGOTO &&
                        currentCommand.Parameters.Where(p => ((ScriptSectionScriptParameter)p).Section is not null).Any(p => ((ScriptSectionScriptParameter)p).Section.Name == Section))
                    {
                        // -1 bc section is about to be incremented after we break
                        curSectionIndex = commandTree.Keys.ToList()
                            .IndexOf(commandTree.Keys.First(s => s.Name == Section)) - 1;
                        break;
                    }
                    else if (currentCommand.Verb == CommandVerb.SELECT &&
                        currentCommand.Parameters.Where(p => p.Type == ScriptParameter.ParameterType.OPTION).Any(p => ((OptionScriptParameter)p).Option.Id == (labels.Objects.FirstOrDefault(f => f.Name.Replace("/", "") == Section)?.Id
                        ?? labels.Objects.Skip(1).First().Id)))
                    {
                        // -1 bc section is about to be incremented after we break
                        curSectionIndex = commandTree.Keys.ToList()
                            .IndexOf(commandTree.Keys.First(s => s.Name == Section)) - 1;
                        break;
                    }
                }

                curSectionIndex++;
            } while (!(commandTree.Keys.ElementAt(curSectionIndex - 1).Name == Section && curCommandIndex == Index)
                && curSectionIndex < commandTree.Count);

            return commands;
        }

        private static List<ScriptParameter> GetScriptParameters(ScriptCommandInvocation invocation, EventFile eventFile, Project project)
        {
            List<ScriptParameter> parameters = new();
            MessageInfoFile messageInfo = project.Dat.Files.First(f => f.Name == "MESSINFOS").CastTo<MessageInfoFile>();

            for (int i = 0; i < invocation.Parameters.Count; i++)
            {
                //todo add additional parameter types for indexed references to other items
                // also, color pickers, special types for things like DS screen location, etc
                // these can then be represented by special controls
                short parameter = invocation.Parameters[i];
                switch ((CommandVerb)Enum.Parse(typeof(CommandVerb), invocation.Command.Mnemonic))
                {
                    case CommandVerb.DIALOGUE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new DialogueScriptParameter("Dialogue", GetDialogueLine(parameter, eventFile)));
                                break;
                            case 1:
                                parameters.Add(new SpriteScriptParameter("Sprite", (CharacterSpriteItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Character_Sprite && (parameter + 1) == ((CharacterSpriteItem)i).Index)));
                                break;
                            case 2:
                                parameters.Add(new SpriteEntranceScriptParameter("Sprite Entrance Transition", parameter));
                                break;
                            case 3:
                                parameters.Add(new SpriteExitScriptParameter("Sprite Exit/Move Transition", parameter));
                                break;
                            case 4:
                                parameters.Add(new SpriteShakeScriptParameter("Sprite Shake", parameter));
                                break;
                            case 5:
                                parameters.Add(new VoiceLineScriptParameter("Voice File", parameter));
                                break;
                            case 6:
                                parameters.Add(new DialoguePropertyScriptParameter("Text Voice Font", messageInfo.MessageInfos[parameter]));
                                break;
                            case 7:
                                parameters.Add(new DialoguePropertyScriptParameter("Text Speed", messageInfo.MessageInfos[parameter]));
                                break;
                            case 8:
                                parameters.Add(new TextEntranceEffectScriptParameter("Text Entrance Effect", parameter));
                                break;
                            case 9:
                                parameters.Add(new ShortScriptParameter("Sprite Layer", parameter));
                                break;
                            case 10:
                                parameters.Add(new BoolScriptParameter("Continue Immediately", parameter == 1));
                                break;
                            case 11:
                                parameters.Add(new BoolScriptParameter("Disable Lip Flap", parameter == 1));
                                break;

                        }
                        break;
                    case CommandVerb.KBG_DISP:
                        if (i == 0)
                        {
                            parameters.Add(new BgScriptParameter("\"Kinetic\" Background", (BackgroundItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter), kinetic: true));
                        }
                        break;
                    case CommandVerb.PIN_MNL:
                        if (i == 0)
                        {
                            parameters.Add(new DialogueScriptParameter("Dialogue", GetDialogueLine(parameter, eventFile)));
                        }
                        break;
                    case CommandVerb.BG_DISP:
                    case CommandVerb.BG_DISP2:
                        if (i == 0)
                        {
                            parameters.Add(new BgScriptParameter("Background", (BackgroundItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter), kinetic: false));
                        }
                        break;
                    case CommandVerb.SCREEN_FADEIN:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("Fade Time (Frames)", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("Fade In Percentage", parameter));
                                break;
                            case 2:
                                parameters.Add(new ScreenScriptParameter("Location", parameter));
                                break;
                            case 3:
                                parameters.Add(new ColorMonochromeScriptParameter("Color", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SCREEN_FADEOUT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("Fade Time (Frames)", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("Fade Out Percentage", parameter));
                                break;
                            case 2:
                                parameters.Add(new ColorScriptParameter("Custom Color", parameter));
                                break;
                            case 3:
                                ((ColorScriptParameter)parameters.Last()).SetGreen(parameter);
                                break;
                            case 4:
                                ((ColorScriptParameter)parameters.Last()).SetBlue(parameter);
                                break;
                            case 5:
                                parameters.Add(new ScreenScriptParameter("Location", parameter));
                                break;
                            case 6:
                                parameters.Add(new ColorMonochromeScriptParameter("Color", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SCREEN_FLASH:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("Fade In Time (Frames)", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("Hold Time (Frames)", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("Fade Out Time (Frames)", parameter));
                                break;
                            case 3:
                                parameters.Add(new ColorScriptParameter("Color", parameter));
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
                                parameters.Add(new SfxScriptParameter("Sound", parameter));
                                break;
                            case 1:
                                parameters.Add(new SfxModeScriptParameter("Mode", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("Volume", parameter));
                                break;
                            case 3:
                                //parameters.Add(new ShortScriptParameter("crossfadeDupe", parameter));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter("Crossfade Time (Frames)", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.BGM_PLAY:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("BGM", parameter)); //BgmScriptParameter("bgmIndex", (BackgroundMusicItem)project.Items.First(i => i.Type == ItemDescription.ItemType.BGM && ((BackgroundMusicItem)i).Index == parameter)));
                                break;
                            case 1:
                                parameters.Add(new BgmModeScriptParameter("Mode", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("Volume", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("Fade In Time (Frames)", parameter));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter("Fade Out Time (Frames)", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.VCE_PLAY:
                        if (i == 0)
                        {
                            parameters.Add(new VoiceLineScriptParameter("Voice File", parameter));
                        }
                        break;
                    case CommandVerb.FLAG:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new FlagScriptParameter("Flag", parameter, global: false));
                                break;
                            case 1:
                                parameters.Add(new BoolScriptParameter("Set/Clear", parameter == 1));
                                break;
                        }
                        break;
                    case CommandVerb.TOPIC_GET:
                        if (i == 0)
                        {
                            parameters.Add(new TopicScriptParameter("Topic", parameter));
                        }
                        break;
                    case CommandVerb.TOGGLE_DIALOGUE:
                        if (i == 0)
                        {
                            parameters.Add(new BoolScriptParameter("Show", parameter == 1));
                        }
                        break;
                    case CommandVerb.SELECT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new OptionScriptParameter("Option 1", eventFile.ChoicesSection.Objects[parameter + 1]));
                                break;
                            case 1:
                                parameters.Add(new OptionScriptParameter("Option 2", eventFile.ChoicesSection.Objects[parameter + 1]));
                                break;
                            case 2:
                                parameters.Add(new OptionScriptParameter("Option 3", eventFile.ChoicesSection.Objects[parameter + 1]));
                                break;
                            case 3:
                                parameters.Add(new OptionScriptParameter("Option 4", eventFile.ChoicesSection.Objects[parameter + 1]));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter("unknown08", parameter));
                                break;
                            case 5:
                                parameters.Add(new ShortScriptParameter("unknown0A", parameter));
                                break;
                            case 6:
                                parameters.Add(new ShortScriptParameter("unknown0C", parameter));
                                break;
                            case 7:
                                parameters.Add(new ShortScriptParameter("unknown0E", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SCREEN_SHAKE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("Duration (Frames)", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("Horizontal Intensity", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("Vertical Intensity", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.GOTO:
                        if (i == 0)
                        {
                            //todo script section labels?
                            parameters.Add(new ScriptSectionScriptParameter("Script Section", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                        }
                        break;
                    case CommandVerb.SCENE_GOTO:
                    case CommandVerb.SCENE_GOTO2:
                        if (i == 0)
                        {
                            parameters.Add(new ConditionalScriptParameter("Conditional", eventFile.ConditionalsSection.Objects[parameter]));
                        }
                        break;
                    case CommandVerb.WAIT:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("Wait Time (Frames)", parameter));
                        }
                        break;
                    case CommandVerb.VGOTO:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ConditionalScriptParameter("Conditional", eventFile.ConditionalsSection.Objects[parameter]));
                                break;
                            // 1 is unused
                            case 2:
                                parameters.Add(new ScriptSectionScriptParameter("Script Section", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                        }
                        break;
                    case CommandVerb.HARUHI_METER:
                        switch (i)
                        {
                            // 0 is unused
                            case 1:
                                parameters.Add(new ShortScriptParameter("Add", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("Set", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.HARUHI_METER_NOSHOW:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("Add", parameter));
                        }
                        break;
                    case CommandVerb.BG_PALEFFECT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new PaletteEffectScriptParameter("Mode", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("Time (Frames)", parameter));
                                break;
                            case 2:
                                parameters.Add(new BoolScriptParameter("Unknown", parameter > 0));
                                break;
                        }
                        break;
                    case CommandVerb.BG_FADE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgScriptParameter("Background (Temp)", (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter), kinetic: false));
                                break;
                            case 1:
                                parameters.Add(new BgScriptParameter("Background (Permanent)", (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter), kinetic: false));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("Fade Time (Frames)", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.TRANS_OUT:
                    case CommandVerb.TRANS_IN:
                        if (i == 0)
                        {
                            parameters.Add(new TransitionScriptParameter("Transition", parameter));
                        }
                        break;
                    case CommandVerb.SET_PLACE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BoolScriptParameter("Display?", parameter == 1));
                                break;
                            case 1:
                                parameters.Add(new PlaceScriptParameter("Place", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.ITEM_DISPIMG:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ItemScriptParameter("Item", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("X", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("Y", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.LOAD_ISOMAP:
                        if (i == 0)
                        {
                            parameters.Add(new MapScriptParameter("Map", (MapItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Map && (parameter) == ((MapItem)i).Map.Index)));
                        }
                        break;
                    case CommandVerb.INVEST_START:
                        switch (i)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                parameters.Add(new ShortScriptParameter($"unknown0{i}", parameter));
                                break;
                            case 4:
                                parameters.Add(new ScriptSectionScriptParameter("End Script Section", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                        }
                        break;
                    case CommandVerb.CHIBI_EMOTE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChibiScriptParameter("Chibi", (ChibiItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi && (parameter) == ((ChibiItem)i).ChibiIndex)));
                                break;
                            case 1:
                                parameters.Add(new ChibiEmoteScriptParameter("Emote", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SKIP_SCENE:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("Scenes to Skip", parameter));
                        }
                        break;
                    case CommandVerb.GLOBAL:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new FlagScriptParameter("Global", parameter, global: true));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("Value", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHIBI_ENTEREXIT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChibiScriptParameter("Chibi", (ChibiItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi && (parameter) == ((ChibiItem)i).ChibiIndex)));
                                break;
                            case 1:
                                parameters.Add(new ChibiEnterExitScriptParameter("Enter/Exit", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("Delay (Frames)", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.GLOBAL2D:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("Value", parameter));
                        }
                        break;
                    case CommandVerb.CHESS_LOAD:
                        if (i == 0)
                        {
                            parameters.Add(new ChessFileScriptParameter("Chess File", parameter));
                        }
                        break;
                    case CommandVerb.CHESS_VGOTO:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ScriptSectionScriptParameter("Clear Block", eventFile.ScriptSections.FirstOrDefault(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                            case 1:
                                parameters.Add(new ScriptSectionScriptParameter("Miss Block", eventFile.ScriptSections.FirstOrDefault(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                            case 2:
                                parameters.Add(new ScriptSectionScriptParameter("Miss 2 Block", eventFile.ScriptSections.FirstOrDefault(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_MOVE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChessSpaceScriptParameter("White Space Begin", parameter));
                                break;
                            case 1:
                                parameters.Add(new ChessSpaceScriptParameter("White Space End", parameter));
                                break;
                            case 2:
                                parameters.Add(new ChessSpaceScriptParameter("Black Space Begin", parameter));
                                break;
                            case 3:
                                parameters.Add(new ChessSpaceScriptParameter("Black Space End", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_GUIDE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChessPieceScriptParameter("Piece 1", parameter));
                                break;
                            case 1:
                                parameters.Add(new ChessPieceScriptParameter("Piece 2", parameter));
                                break;
                            case 2:
                                parameters.Add(new ChessPieceScriptParameter("Piece 3", parameter));
                                break;
                            case 3:
                                parameters.Add(new ChessPieceScriptParameter("Piece 4", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_HIGHLIGHT:
                        if (parameter != -1)
                        {
                            parameters.Add(new ChessSpaceScriptParameter($"Highlight Space {i}", parameter));
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_CROSS:
                        if (parameter != -1)
                        {
                            parameters.Add(new ChessSpaceScriptParameter($"Cross Space {i}", parameter));
                        }
                        break;
                    case CommandVerb.EPHEADER:
                        if (i == 0)
                        {
                            parameters.Add(new EpisodeHeaderScriptParameter("Episode Header", parameter));
                        }
                        break;
                    case CommandVerb.CONFETTI:
                        if (i == 0)
                        {
                            parameters.Add(new BoolScriptParameter("Visible?", parameter == 1));
                        }
                        break;
                    case CommandVerb.BG_DISPTEMP:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgScriptParameter("Background", (BackgroundItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter), kinetic: false));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("Unknown", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.BG_SCROLL:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgScrollDirectionScriptParameter("Scroll Direction", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("Scroll Speed", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.WAIT_CANCEL:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("Wait Time (Frames)", parameter));
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
                str += $" {((DialogueScriptParameter)Parameters[0]).Line.Text[0..Math.Min(((DialogueScriptParameter)Parameters[0]).Line.Text.Length, 10)]}...";
            }
            return str;
        }

        private static DialogueLine GetDialogueLine(short index, EventFile eventFile)
        {
            return eventFile.DialogueLines[index];
        }
    }
}
