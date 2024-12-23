using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ChibiEmoteScriptParameter : ScriptParameter
{
    public ChibiEmote Emote { get; set; }
    public override short[] GetValues(object obj = null) => new short[] { (short)Emote };

    public override string GetValueString(Project project)
    {
        return project.Localize(Emote.ToString());
    }

    public ChibiEmoteScriptParameter(string name, short emote) : base(name, ParameterType.CHIBI_EMOTE)
    {
        Emote = (ChibiEmote)emote;
    }

    public override ChibiEmoteScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Emote);
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
