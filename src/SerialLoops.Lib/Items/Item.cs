namespace SerialLoops.Lib.Items
{
    public abstract class Item : ItemDescription
    {

        public Item(string name, ItemType type) : base(name, type)
        {
        }

        public abstract void Refresh(Project project);
    }

    public class NoneItem : Item
    {
        public static readonly NoneItem VOICE = new(ItemType.Voice); 

        public NoneItem(ItemType type) : base("NONE", type)
        {
        }

        public override void Refresh(Project project)
        {
        }
    }
}