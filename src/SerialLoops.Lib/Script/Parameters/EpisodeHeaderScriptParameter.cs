using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class EpisodeHeaderScriptParameter : ScriptParameter
    {
        public Episode EpisodeHeaderIndex { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)EpisodeHeaderIndex };

        public EpisodeHeaderScriptParameter(string name, short epHeaderIndex) : base(name, ParameterType.EPISODE_HEADER)
        {
            EpisodeHeaderIndex = (Episode)epHeaderIndex;
        }

        public override EpisodeHeaderScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, (short)EpisodeHeaderIndex);
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
