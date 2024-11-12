using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items;

public abstract class Item(string name, ItemDescription.ItemType type, string displayName = "")
    : ItemDescription(name, type, displayName)
{
    public abstract void Refresh(Project project, ILogger log);
}

public class NoneItem(ItemDescription.ItemType type) : Item("NONE", type)
{
    public static readonly NoneItem VOICE = new(ItemType.Voice); 
    public static readonly NoneItem SCRIPT = new(ItemType.Script);

    public override void Refresh(Project project, ILogger log)
    {
    }
}