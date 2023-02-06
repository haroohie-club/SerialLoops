﻿namespace SerialLoops.Lib.Script
{
    public class ChibiEmoteScriptParameter : ScriptParameter
    {
        public ChibiEmote Emote { get; set; }

        public ChibiEmoteScriptParameter(string name, short emote) : base(name, ParameterType.CHIBI_EMOTE)
        {
            Emote = (ChibiEmote)emote;
        }

        public enum ChibiEmote : short
        {
            EXCLAMATION_POINT = 1,
            LIGHT_BULB = 2,
            ANGER_MARK = 3,
            MUSIC_NOTE = 4,
            SWEAT_DROP = 5,
        }
    }
}
