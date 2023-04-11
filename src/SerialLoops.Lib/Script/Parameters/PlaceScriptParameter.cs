using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using System.Linq;

namespace SerialLoops.Lib.Script.Parameters
{
    public class PlaceScriptParameter : ScriptParameter
    {
        public PlaceItem Place { get; set; }
        public override short[] GetValues(object obj = null) => new short[] { (short)Place.Index };

        public PlaceScriptParameter(string name, PlaceItem place) : base(name, ParameterType.PLACE)
        {
            Place = place;
        }

        public override PlaceScriptParameter Clone(Project project, EventFile eventFile)
        {
            return new(Name, Place);
        }
    }
}
