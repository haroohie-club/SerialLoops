using SerialLoops.Lib.Items;
using System.Linq;

namespace SerialLoops.Lib.Script.Parameters
{
    public class PlaceScriptParameter : ScriptParameter
    {
        public PlaceItem Place { get; set; }

        public PlaceScriptParameter(string name, PlaceItem place) : base(name, ParameterType.PLACE)
        {
            Place = place;
        }

        public override PlaceScriptParameter Clone()
        {
            return new(Name, Place);
        }
    }
}
