using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Lib.Items
{
    public class ItemDescription
    {

        public string Name { get; private set; }
        public ItemType Type { get; private set; }

        public ItemDescription(string name, ItemType type)
        {
            Name = name;
            Type = type;
        }

        // Load an item from an ItemDescription
        public Item Load()
        {
            throw new NotImplementedException();
        }

        // Enum with values for each type of item
        public enum ItemType
        {
            Map,
            Dialogue
        }

    }
}
