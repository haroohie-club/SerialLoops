﻿namespace SerialLoops.Lib.Script
{
    public abstract class ScriptParameter
    {
        public ParameterType Type { get; protected set; }
        public string Name { get; protected set; }

        protected ScriptParameter(string name, ParameterType type)
        {
            Name = name;
            Type = type;
        }
        
        public enum ParameterType
        {
            BG,
            BG_SCROLL_DIRECTION,
            BGM,
            BGM_MODE,
            BOOL,
            CHESS_FILE,
            CHESS_PIECE,
            CHESS_SPACE,
            CHIBI,
            CHIBI_EMOTE,
            CHIBI_ENTER_EXIT,
            COLOR,
            COLOR_MONOCHROME,
            CONDITIONAL,    // also used for SCENE_GOTO script names
            DIALOGUE_PROPERTY,    // MESSINFO stuff, voice font & text speed
            EPISODE_HEADER,
            FLAG,
            GLOBAL,
            ITEM,
            MAP,
            OPTION,
            PALETTE_EFFECT,
            PLACE,
            SCREEN,
            SCRIPT_BLOCK,
            SFX,
            SFX_MODE,
            SHORT,
            SPRITE,
            SPRITE_ENTRANCE,
            SPRITE_EXIT,
            SPRITE_SHAKE,
            STRING,
            TEXT_ENTRANCE_EFFECT,
            TOPIC,
            TRANSITION,
            VOICE_LINE,
        }
    }
}
