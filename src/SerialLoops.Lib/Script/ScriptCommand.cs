using HaruhiChokuretsuLib.Archive.Event;
using System;
using System.Collections.Generic;

namespace SerialLoops.Lib.Script
{
    public class ScriptCommand
    {
        public CommandVerb Verb { get; set; }

        public List<ScriptParameter> Parameters { get; set; }

        public static ScriptCommand FromInvocation(ScriptCommandInvocation invocation, EventFile eventFile)
        {
            return new()
            {
                Verb = (CommandVerb)Enum.Parse(typeof(CommandVerb), invocation.Command.Mnemonic),
                Parameters = GetScriptParameters(invocation, eventFile)
            };
        }

        private static List<ScriptParameter> GetScriptParameters(ScriptCommandInvocation invocation, EventFile eventFile)
        {
            List<ScriptParameter> parameters = new();

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
                                parameters.Add(new StringScriptParameter("dialogue", StringScriptParameter.StringParameterType.DIALOGUE, GetDialogueLine(parameter, eventFile)));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("spriteIndex", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("spriteEntranceTransition", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("spriteExitOrInternalTransition", parameter));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter("spriteShake", parameter));
                                break;
                            case 5:
                                parameters.Add(new ShortScriptParameter("voiceIndex", parameter));
                                break;
                            case 6:
                                parameters.Add(new ShortScriptParameter("textVoiceFont", parameter));
                                break;
                            case 7:
                                parameters.Add(new ShortScriptParameter("textSpeed", parameter));
                                break;
                            case 8:
                                parameters.Add(new ShortScriptParameter("textEntranceEffect", parameter));
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
                            parameters.Add(new ShortScriptParameter("kbgIndex", parameter));
                        }
                        break;
                    case CommandVerb.PIN_MNL:
                        if (i == 0)
                        {
                            parameters.Add(new StringScriptParameter("dialogue", StringScriptParameter.StringParameterType.DIALOGUE, GetDialogueLine(parameter, eventFile)));
                        }
                        break;
                    case CommandVerb.BG_DISP:
                    case CommandVerb.BG_DISP2:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("bgIndex", parameter));
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
                                parameters.Add(new ShortScriptParameter("fadeLocation", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("fadeColor", parameter));
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
                                parameters.Add(new ShortScriptParameter("fadeColorRed", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("fadeColorGreen", parameter));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter("fadeColorBlue", parameter));
                                break;
                            case 5:
                                parameters.Add(new ShortScriptParameter("fadeLocation", parameter));
                                break;
                            case 6:
                                parameters.Add(new ShortScriptParameter("fadeColor", parameter));
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
                                parameters.Add(new ShortScriptParameter("flashColorRed", parameter));
                                break;
                            case 4:
                                parameters.Add(new ShortScriptParameter("flashColorGreen", parameter));
                                break;
                            case 5:
                                parameters.Add(new ShortScriptParameter("flashColorBlue", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.SND_PLAY:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("soundIndex", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("mode", parameter));
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
                                parameters.Add(new ShortScriptParameter("bgmIndex", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("mode", parameter));
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
                            parameters.Add(new ShortScriptParameter("vceIndex", parameter));
                        }
                        break;
                    case CommandVerb.FLAG:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("flag", parameter));
                                break;
                            case 1:
                                parameters.Add(new BoolScriptParameter("set", parameter == 1));
                                break;
                        }
                        break;
                    case CommandVerb.TOPIC_GET:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("topicId", parameter));
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
                                parameters.Add(new ShortScriptParameter("option1", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("option2", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("option3", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("option4", parameter));
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
                            parameters.Add(new ShortScriptParameter("blockId", parameter));
                        }
                        break;
                    case CommandVerb.SCENE_GOTO:
                    case CommandVerb.SCENE_GOTO2:
                        if (i == 0)
                        {
                            //todo script conditions
                            parameters.Add(new ShortScriptParameter("conditionalIndex", parameter));
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
                                parameters.Add(new ShortScriptParameter("conditionalIndex", parameter));
                                break;
                            // 1 is unused
                            case 2:
                                parameters.Add(new ShortScriptParameter("gotoId", parameter));
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
                                parameters.Add(new ShortScriptParameter("paletteMode", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("transitionTime", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("unknownBool", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.BG_FADE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("bgIndex", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("bgIndexSuper", parameter));
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
                            parameters.Add(new ShortScriptParameter("index", parameter));
                        }
                        break;
                    case CommandVerb.SET_PLACE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new BoolScriptParameter("display", parameter == 1));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("placeIndex", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.ITEM_DISPIMG:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("itemIndex", parameter));
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
                            parameters.Add(new ShortScriptParameter("mapFileIndex", parameter));
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
                                parameters.Add(new ShortScriptParameter("endScriptBlock", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHIBI_EMOTE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("chibiIndex", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("emoteIndex", parameter));
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
                                parameters.Add(new ShortScriptParameter("globalIndex", parameter));
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
                                parameters.Add(new ShortScriptParameter("chibiIndex", parameter));
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
                            parameters.Add(new ShortScriptParameter("chessFileIndex", parameter));
                        }
                        break;
                    case CommandVerb.CHESS_VGOTO:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("clearBlock", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("missBlock", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("miss2Block", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_MOVE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("whiteSpaceBegin", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("whiteSpaceEnd", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("blackSpaceBegin", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("blackSpaceEnd", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_GUIDE:
                        switch (i)
                        {
                            case 0:
                                parameters.Add(new ShortScriptParameter("piece1", parameter));
                                break;
                            case 1:
                                parameters.Add(new ShortScriptParameter("piece2", parameter));
                                break;
                            case 2:
                                parameters.Add(new ShortScriptParameter("piece3", parameter));
                                break;
                            case 3:
                                parameters.Add(new ShortScriptParameter("piece4", parameter));
                                break;
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_HIGHLIGHT:
                        if (parameter != -1)
                        {
                            parameters.Add(new ShortScriptParameter($"highlightSpace{i}", parameter));
                        }
                        break;
                    case CommandVerb.CHESS_TOGGLE_CROSS:
                        if (parameter != -1)
                        {
                            parameters.Add(new ShortScriptParameter($"crossSpace{i}", parameter));
                        }
                        break;
                    case CommandVerb.EPHEADER:
                        if (i == 0)
                        {
                            parameters.Add(new ShortScriptParameter("headerIndex", parameter));
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
                                parameters.Add(new ShortScriptParameter("bgIndex", parameter));
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
                                parameters.Add(new ShortScriptParameter("scrollDirection", parameter));
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

        private static string GetDialogueLine(short index, EventFile eventFile)
        {
            return eventFile.DialogueLines[index].Text;
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
