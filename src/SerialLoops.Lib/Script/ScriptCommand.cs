using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace SerialLoops.Lib.Script
{
    public class ScriptItemCommand
    {
        public CommandVerb Verb { get; set; }

        public List<ScriptParameter> Parameters { get; set; }

        public static ScriptItemCommand FromInvocation(ScriptCommandInvocation invocation, EventFile eventFile, Project project)
        {
            return new()
            {
                Verb = (CommandVerb)Enum.Parse(typeof(CommandVerb), invocation.Command.Mnemonic),
                Parameters = GetScriptParameters(invocation, eventFile, project)
            };
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
                                parameters.Add(new DialogueScriptParameter("dialogue", GetDialogueLine(parameter, eventFile)));
                                break;
                            case 1:
                                parameters.Add(new SpriteScriptParameter("spriteIndex", (CharacterSpriteItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Character_Sprite && (parameter + 1) == ((CharacterSpriteItem)i).Index)));
                                break;
                            case 2:
                                parameters.Add(new SpriteEntranceScriptParameter("spriteEntranceTransition", parameter));
                                break;
                            case 3:
                                parameters.Add(new SpriteExitScriptParameter("spriteExitOrInternalTransition", parameter));
                                break;
                            case 4:
                                parameters.Add(new SpriteShakeScriptParameter("spriteShake", parameter));
                                break;
                            case 5:
                                parameters.Add(new VoiceLineScriptParameter("voiceIndex", parameter));
                                break;
                            case 6:
                                parameters.Add(new DialoguePropertyScriptParameter("textVoiceFont", messageInfo.MessageInfos[parameter]));
                                break;
                            case 7:
                                parameters.Add(new DialoguePropertyScriptParameter("textSpeed", messageInfo.MessageInfos[parameter]));
                                break;
                            case 8:
                                parameters.Add(new TextEntranceEffectScriptParameter("textEntranceEffect", parameter));
                                break;
                            case 9:
                                parameters.Add(new ShortScriptParameter("spriteLayer", parameter));
                                break;
                            case 10:
                                parameters.Add(new BoolScriptParameter("continue", parameter == 1));
                                break;
                            case 11:
                                parameters.Add(new BoolScriptParameter("noLipFlap", parameter == 1));
                                break;

                        }
                        break;
                    case CommandVerb.KBG_DISP:
                        if (i == 0)
                        {
                            parameters.Add(new BgScriptParameter("kbgIndex", (BackgroundItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter)));
                        }
                        break;
                    case CommandVerb.PIN_MNL:
                        if (i == 0)
                        {
                            parameters.Add(new DialogueScriptParameter("dialogue", GetDialogueLine(parameter, eventFile)));
                        }
                        break;
                    case CommandVerb.BG_DISP:
                    case CommandVerb.BG_DISP2:
                        if (i == 0)
                        {
                            parameters.Add(new BgScriptParameter("bgIndex", (BackgroundItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter)));
                        }
                        break;
                    case CommandVerb.SCREEN_FADEIN:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("timeToFade", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("fadeInToPercentage", parameter));
                                break;
                            case 2:
                                parameters.Add(new ScreenScriptParameter("fadeLocation", parameter));
                                break;
                            case 3:
                                parameters.Add(new ColorMonochromeScriptParameter("fadeColor", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SCREEN_FADEOUT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("timeToFade", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("fadeInToPercentage", parameter));
                                break;
                            case 2:
                                parameters.Add(new ColorScriptParameter("fadeColorCustom", parameter));
                                break;
                            case 3:
                                ((ColorScriptParameter)parameters.Last()).SetGreen(parameter);
                                break;
                            case 4:
                                ((ColorScriptParameter)parameters.Last()).SetBlue(parameter);
                                break;
                            case 5:
                                parameters.Add(new ScreenScriptParameter("fadeLocation", parameter));
                                break;
                            case 6:
                                parameters.Add(new ColorMonochromeScriptParameter("fadeColor", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SCREEN_FLASH:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("fadeInTime", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("holdTime", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("fadeOutTime", parameter));
                                break;
                            case 3:
                                parameters.Add(new ColorScriptParameter("flashColor", parameter));
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
                                parameters.Add(new SfxScriptParameter("soundIndex", parameter));
                                break;
                            case 1:
                                parameters.Add(new SfxModeScriptParameter("mode", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("volume", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("crossfadeDupe", parameter));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter("crossfadeTime", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.BGM_PLAY:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("bgmIndex", parameter)); //BgmScriptParameter("bgmIndex", (BackgroundMusicItem)project.Items.First(i => i.Type == ItemDescription.ItemType.BGM && ((BackgroundMusicItem)i).Index == parameter)));
                                break;
                            case 1:
                                parameters.Add(new BgmModeScriptParameter("mode", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("volume", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("fadeInTime", parameter));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter("fadeOutTime", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.VCE_PLAY:
                        if (i == 0)
                        {
                            parameters.Add(new VoiceLineScriptParameter("vceIndex", parameter));
                        }
                        break;
                    case CommandVerb.FLAG:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new FlagScriptParameter("flag", parameter, global: false));
                                break;
                            case 1:
                                parameters.Add(new BoolScriptParameter("set", parameter == 1));
                                break;
                        }
                        break;
                    case CommandVerb.TOPIC_GET:
                        if (i == 0)
                        {
                            parameters.Add(new TopicScriptParameter("topicId", parameter));
                        }
                        break;
                    case CommandVerb.TOGGLE_DIALOGUE:
                        if (i == 0)
                        {
                            parameters.Add(new BoolScriptParameter("show", parameter == 1));
                        }
                        break;
                    case CommandVerb.SELECT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new OptionScriptParameter("option1", eventFile.ChoicesSection.Objects[parameter + 1]));
                                break;
                            case 1:
                                parameters.Add(new OptionScriptParameter("option2", eventFile.ChoicesSection.Objects[parameter + 1]));
                                break;
                            case 2:
                                parameters.Add(new OptionScriptParameter("option3", eventFile.ChoicesSection.Objects[parameter + 1]));
                                break;
                            case 3:
                                parameters.Add(new OptionScriptParameter("option4", eventFile.ChoicesSection.Objects[parameter + 1]));
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
                                parameters.Add(new ShortScriptParameter("duration", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("horizontalIntensity", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("verticalIntensity", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.GOTO:
                        if (i == 0)
                        {
                            //todo script section labels?
                            parameters.Add(new ScriptSectionScriptParameter("blockId", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("/", ""))));
                        }
                        break;
                    case CommandVerb.SCENE_GOTO:
                    case CommandVerb.SCENE_GOTO2:
                        if (i == 0)
                        {
                            parameters.Add(new ConditionalScriptParameter("conditionalIndex", eventFile.ConditionalsSection.Objects[parameter]));
                        }
                        break;
                    case CommandVerb.WAIT:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("frames", parameter));
                        }
                        break;
                    case CommandVerb.VGOTO:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ConditionalScriptParameter("conditionalIndex", eventFile.ConditionalsSection.Objects[parameter]));
                                break;
                            // 1 is unused
                            case 2:
                                parameters.Add(new ScriptSectionScriptParameter("gotoId", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name)));
                                break;
                        }
                        break;
                    case CommandVerb.HARUHI_METER:
                        switch (i)
                        {
                            // 0 is unused
                            case 1:
                                parameters.Add(new ShortScriptParameter("addValue", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("setValue", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.HARUHI_METER_NOSHOW:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("addValue", parameter));
                        }
                        break;
                    case CommandVerb.BG_PALEFFECT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new PaletteEffectScriptParameter("paletteMode", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("transitionTime", parameter));
                                break;
                            case 2:
                                parameters.Add(new BoolScriptParameter("unknownBool", parameter > 0));
                                break;
                        }
                        break;
                    case CommandVerb.BG_FADE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgScriptParameter("bgIndex", (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter)));
                                break;
                            case 1:
                                parameters.Add(new BgScriptParameter("bgIndexSuper", (BackgroundItem)project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter)));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("fadeTime", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.TRANS_OUT:
                    case CommandVerb.TRANS_IN:
                        if (i == 0)
                        {
                            parameters.Add(new TransitionScriptParameter("index", parameter));
                        }
                        break;
                    case CommandVerb.SET_PLACE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BoolScriptParameter("display", parameter == 1));
                                break;
                            case 1:
                                parameters.Add(new PlaceScriptParameter("placeIndex", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.ITEM_DISPIMG:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ItemScriptParameter("itemIndex", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("x", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("y", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.LOAD_ISOMAP:
                        if (i == 0)
                        {
                            parameters.Add(new MapScriptParameter("mapFileIndex", (MapItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Map && (parameter) == ((MapItem)i).QmapIndex)));
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
                                parameters.Add(new ScriptSectionScriptParameter("endScriptBlock", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("NONE/", ""))));
                                break;
                        }
                        break;
                    case CommandVerb.CHIBI_EMOTE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChibiScriptParameter("chibiIndex", (ChibiItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi && (parameter) == ((ChibiItem)i).ChibiIndex)));
                                break;
                            case 1:
                                parameters.Add(new ChibiEmoteScriptParameter("emoteIndex", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SKIP_SCENE:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("scenesToSkip", parameter));
                        }
                        break;
                    case CommandVerb.GLOBAL:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new FlagScriptParameter("globalIndex", parameter, global: true));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("value", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHIBI_ENTEREXIT:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChibiScriptParameter("chibiIndex", (ChibiItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi && (parameter) == ((ChibiItem)i).ChibiIndex)));
                                break;
                            case 1:
                                parameters.Add(new BoolScriptParameter("entering", parameter != 1));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("delay", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.GLOBAL2D:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("value", parameter));
                        }
                        break;
                    case CommandVerb.CHESS_LOAD:
                        if (i == 0)
                        {
                            parameters.Add(new ChessFileScriptParameter("chessFileIndex", parameter));
                        }
                        break;
                    case CommandVerb.CHESS_VGOTO:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ScriptSectionScriptParameter("clearBlock", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("NONE/", ""))));
                                break;
                            case 1:
                                parameters.Add(new ScriptSectionScriptParameter("missBlock", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("NONE/", ""))));
                                break;
                            case 2:
                                parameters.Add(new ScriptSectionScriptParameter("miss2Block", eventFile.ScriptSections.First(s => s.Name == eventFile.LabelsSection.Objects.First(l => l.Id == parameter).Name.Replace("NONE/", ""))));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_MOVE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChessSpaceScriptParameter("whiteSpaceBegin", parameter));
                                break;
                            case 1:
                                parameters.Add(new ChessSpaceScriptParameter("whiteSpaceEnd", parameter));
                                break;
                            case 2:
                                parameters.Add(new ChessSpaceScriptParameter("blackSpaceBegin", parameter));
                                break;
                            case 3:
                                parameters.Add(new ChessSpaceScriptParameter("blackSpaceEnd", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_GUIDE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ChessPieceScriptParameter("piece1", parameter));
                                break;
                            case 1:
                                parameters.Add(new ChessPieceScriptParameter("piece2", parameter));
                                break;
                            case 2:
                                parameters.Add(new ChessPieceScriptParameter("piece3", parameter));
                                break;
                            case 3:
                                parameters.Add(new ChessPieceScriptParameter("piece4", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_HIGHLIGHT:
                        if (parameter != -1)
                        {
                            parameters.Add(new ChessSpaceScriptParameter($"highlightSpace{i}", parameter));
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_CROSS:
                        if (parameter != -1)
                        {
                            parameters.Add(new ChessSpaceScriptParameter($"crossSpace{i}", parameter));
                        }
                        break;
                    case CommandVerb.EPHEADER:
                        if (i == 0)
                        {
                            parameters.Add(new EpisodeHeaderScriptParameter("headerIndex", parameter));
                        }
                        break;
                    case CommandVerb.CONFETTI:
                        if (i == 0)
                        {
                            parameters.Add(new BoolScriptParameter("visible", parameter == 1));
                        }
                        break;
                    case CommandVerb.BG_DISPTEMP:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgScriptParameter("bgIndex", (BackgroundItem)project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Id == parameter)));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("unknown1", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.BG_SCROLL:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BgScrollDirectionScriptParameter("scrollDirection", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("scrollSpeed", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.WAIT_CANCEL:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("frames", parameter));
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

        public enum CommandVerb
        {
            INIT_READ_FLAG,
            DIALOGUE,
            KBG_DISP,
            PIN_MNL,
            BG_DISP,
            SCREEN_FADEIN,
            SCREEN_FADEOUT,
            SCREEN_FLASH,
            SND_PLAY,
            REMOVED,
            UNKNOWN0A,
            BGM_PLAY,
            VCE_PLAY,
            FLAG,
            TOPIC_GET,
            TOGGLE_DIALOGUE,
            SELECT,
            SCREEN_SHAKE,
            SCREEN_SHAKE_STOP,
            GOTO,
            SCENE_GOTO,
            WAIT,
            HOLD,
            NOOP1,
            VGOTO,
            HARUHI_METER,
            HARUHI_METER_NOSHOW,
            BG_PALEFFECT,
            BG_FADE,
            TRANS_OUT,
            TRANS_IN,
            SET_PLACE,
            ITEM_DISPIMG,
            SET_READ_FLAG,
            STOP,
            NOOP2,
            LOAD_ISOMAP,
            INVEST_START,
            INVEST_END,
            CHIBI_EMOTE,
            NEXT_SCENE,
            SKIP_SCENE,
            GLOBAL,
            CHIBI_ENTEREXIT,
            AVOID_DISP,
            GLOBAL2D,
            CHESS_LOAD,
            CHESS_VGOTO,
            CHESS_MOVE,
            CHESS_TOGGLE_GUIDE,
            CHESS_TOGGLE_HIGHLIGHT,
            CHESS_TOGGLE_CROSS,
            CHESS_CLEAR_ANNOTATIONS,
            CHESS_RESET,
            SCENE_GOTO2,
            EPHEADER,
            NOOP3,
            CONFETTI,
            BG_DISPTEMP,
            BG_SCROLL,
            OP_MODE,
            WAIT_CANCEL,
            BG_REVERT,
            BG_DISP2
        }
    }
}
