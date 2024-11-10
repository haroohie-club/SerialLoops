using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Items;

public abstract class Item : ItemDescription
{

    public Item(string name, ItemType type, string displayName = "") : base(name, type, displayName)
    {
    }

    public abstract void Refresh(Project project, ILogger log);
}

public class NoneItem : Item
{
    public static readonly NoneItem VOICE = new(ItemType.Voice); 
    public static readonly NoneItem SCRIPT = new(ItemType.Script);

    public NoneItem(ItemType type) : base("NONE", type)
    {
    }

    public override void Refresh(Project project, ILogger log)
    {
    }
}