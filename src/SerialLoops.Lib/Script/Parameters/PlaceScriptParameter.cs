using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Script.Parameters
{
    public class PlaceScriptParameter : ScriptParameter
    {
        public short PlaceIndex { get; set; }

        public PlaceScriptParameter(string name, short placeIndex) : base(name, ParameterType.PLACE)
        {
            PlaceIndex = placeIndex;
        }
    }
}
