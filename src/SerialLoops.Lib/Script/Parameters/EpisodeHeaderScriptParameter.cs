using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters
{
    public class EpisodeHeaderScriptParameter(string name, short epHeaderIndex) : ScriptParameter(name, ParameterType.EPISODE_HEADER)
    {
        public Episode EpisodeHeaderIndex { get; set; } = (Episode)epHeaderIndex;
        public override short[] GetValues(object obj = null) => new short[] { (short)EpisodeHeaderIndex };

        public override EpisodeHeaderScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, (short)EpisodeHeaderIndex);
        }

        public SystemTextureItem GetTexture(Project project)
        {
            return GetTexture(EpisodeHeaderIndex, project);
        }

        public static SystemTextureItem GetTexture(Episode episode, Project project)
        {
            // We use names here because display names can change but names cannot
            return episode switch
            {
                Episode.EPISODE_1 =>
                    (SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_SYS_CMN_T60"),
                Episode.EPISODE_2 =>
                    (SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_SYS_CMN_T61"),
                Episode.EPISODE_3 =>
                    (SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_SYS_CMN_T62"),
                Episode.EPISODE_4 =>
                    (SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_SYS_CMN_T63"),
                Episode.EPISODE_5 =>
                    (SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_SYS_CMN_T64"),
                Episode.EPILOGUE =>
                    (SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_SYS_CMN_T66"),
                _ => null,
            };
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
