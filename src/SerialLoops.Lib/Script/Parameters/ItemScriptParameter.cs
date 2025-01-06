using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class ItemScriptParameter(string name, short itemIndex) : ScriptParameter(name, ParameterType.ITEM)
{
    public short ItemIndex { get; set; } = itemIndex;
    public override short[] GetValues(object obj = null) => new short[] { ItemIndex };

    public override string GetValueString(Project project)
    {
        return project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Item &&
                                        ((ItemItem)i).ItemIndex == ItemIndex)?.DisplayName;
    }

    public override ItemScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, ItemIndex);
    }
}
