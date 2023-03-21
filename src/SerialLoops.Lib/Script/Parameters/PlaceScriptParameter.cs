namespace SerialLoops.Lib.Script.Parameters
{
    public class PlaceScriptParameter : ScriptParameter
    {
        public short PlaceIndex { get; set; }

        public PlaceScriptParameter(string name, short placeIndex) : base(name, ParameterType.PLACE)
        {
            PlaceIndex = placeIndex;
        }

        public override PlaceScriptParameter Clone()
        {
            return new(Name, PlaceIndex);
        }
    }
}
