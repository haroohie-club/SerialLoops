namespace SerialLoops.Lib.Items
{
    public abstract class Item : ItemDescription
    {

        public Item(string name, ItemType type) : base(name, type)
        {
        }

        public abstract void Refresh(Project project);
    }

}