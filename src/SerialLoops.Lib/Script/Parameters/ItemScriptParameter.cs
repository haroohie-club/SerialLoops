using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ItemScriptParameter(string name, short itemIndex) : ScriptParameter(name, ParameterType.ITEM)
{
    public short ItemIndex { get; set; } = itemIndex;
    public override short[] GetValues(object obj = null) => new short[] { ItemIndex };

    public override ItemScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, ItemIndex);
    }
}