using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class SpriteEntranceScriptParameter : ScriptParameter
    {
        public SpriteEntranceTransition EntranceTransition { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)EntranceTransition };

        public SpriteEntranceScriptParameter(string name, short entranceTransition) : base(name, ParameterType.SPRITE_ENTRANCE)
        {
            EntranceTransition = (SpriteEntranceTransition)entranceTransition;
        }

        public override SpriteEntranceScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, (short)EntranceTransition);
        }

        public enum SpriteEntranceTransition : short
        {
            NO_TRANSITION = 0,
            SLIDE_LEFT_TO_CENTER = 1,
            SLIDE_RIGHT_TO_CENTER = 2,
            SLIDE_LEFT_TO_RIGHT = 3,
            SLIDE_RIGHT_TO_LEFT = 4,
            SLIDE_LEFT_TO_RIGHT_SLOW = 5,
            SLIDE_RIGHT_TO_LEFT_SLOW = 6,
            FADE_TO_CENTER = 7,
            SLIDE_LEFT_TO_RIGHT_FAST = 8,
            SLIDE_RIGHT_TO_LEFT_FAST = 9,
            PEEK_RIGHT_TO_LEFT = 10,
            FADE_IN_LEFT = 11,
        }
    }
}
