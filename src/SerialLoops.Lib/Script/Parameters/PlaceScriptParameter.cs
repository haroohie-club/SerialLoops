using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class PlaceScriptParameter(string name, PlaceItem place) : ScriptParameter(name, ParameterType.PLACE)
{
    public PlaceItem Place { get; set; } = place;
    public override short[] GetValues(object obj = null) => new short[] { (short)Place.Index };

    public override PlaceScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Place);
    }
}