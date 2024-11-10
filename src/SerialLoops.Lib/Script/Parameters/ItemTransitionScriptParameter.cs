using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class ItemTransitionScriptParameter(string name, short itemTransition) : ScriptParameter(name, ParameterType.ITEM_TRANSITION)
{
    public ItemItem.ItemTransition Transition { get; set; } = (ItemItem.ItemTransition)itemTransition;
    public override short[] GetValues(object obj = null) => [(short)Transition];

    public override ItemTransitionScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Transition);
    }
}