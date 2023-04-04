using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters
{
    public class ChessSpaceScriptParameter : ScriptParameter
    {
        public short SpaceIndex { get; set; }

        public ChessSpaceScriptParameter(string name, short spaceIndex) : base(name, ParameterType.CHESS_SPACE)
        {
            SpaceIndex = spaceIndex;
        }

        public override ChessSpaceScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, SpaceIndex);
        }

    }
}
