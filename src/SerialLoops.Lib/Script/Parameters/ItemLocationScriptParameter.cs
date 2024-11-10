using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class ItemLocationScriptParameter(string name, short itemLocation) : ScriptParameter(name, ParameterType.ITEM_LOCATION)
{
    public ItemItem.ItemLocation Location { get; set; } = (ItemItem.ItemLocation)itemLocation;
    public override short[] GetValues(object obj = null) => [(short)Location];

    public override ItemLocationScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Location);
    }
}