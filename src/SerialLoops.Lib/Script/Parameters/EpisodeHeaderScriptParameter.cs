namespace SerialLoops.Lib.Script.Parameters
{
    public class EpisodeHeaderScriptParameter : ScriptParameter
    {
        public Episode EpisodeHeaderIndex { get; set; }

        public EpisodeHeaderScriptParameter(string name, short epHeaderIndex) : base(name, ParameterType.EPISODE_HEADER)
        {
            EpisodeHeaderIndex = (Episode)epHeaderIndex;
        }

        public enum Episode
        {
            None = 0,
            EPISODE_1 = 1,
            EPISODE_2 = 2,
            EPISODE_3 = 3,
            EPISODE_4 = 4,
            EPISODE_5 = 5,
            EPILOGUE = 6,
        }
    }
}
