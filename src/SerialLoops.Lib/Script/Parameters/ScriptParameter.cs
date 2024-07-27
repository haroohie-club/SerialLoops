using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public abstract class ScriptParameter
    {
        public ParameterType Type { get; protected set; }
        public string Name { get; protected set; }
        public abstract short[] GetValues(object obj = null);

        protected ScriptParameter(string name, ParameterType type)
        {
            Name = name;
            Type = type;
        }

        public abstract ScriptParameter Clone(Project project, EventFile eventFile);

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
            CONDITIONAL,
            DIALOGUE,
            CHARACTER,    // MESSINFO stuff, voice font & text speed
            EPISODE_HEADER,
            FLAG,
            FRIENDSHIP_LEVEL,
            ITEM,
            ITEM_LOCATION,
            ITEM_TRANSITION,
            MAP,
            OPTION,
            PALETTE_EFFECT,
            PLACE,
            SCREEN,
            SCRIPT_SECTION,
            SFX,
            SFX_MODE,
            SHORT,
            SPRITE,
            SPRITE_ENTRANCE,
            SPRITE_EXIT,
            SPRITE_SHAKE,
            TEXT_ENTRANCE_EFFECT,
            TOPIC,
            TRANSITION,
            VOICE_LINE,
        }

    }
}
