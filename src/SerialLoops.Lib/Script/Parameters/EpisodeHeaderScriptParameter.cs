﻿namespace SerialLoops.Lib.Script.Parameters
{
    public class EpisodeHeaderScriptParameter : ScriptParameter
    {
        public short EpisodeHeaderIndex { get; set; }

        public EpisodeHeaderScriptParameter(string name, short epHeaderIndex) : base(name, ParameterType.EPISODE_HEADER)
        {
            EpisodeHeaderIndex = epHeaderIndex;
        }

    }
}